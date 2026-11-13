using RealTimeApps.Applications.Models;
using RealTimeApps.Applications.Ports;
using RealTimeApps.Domains.Entities;
using RealTimeApps.Domains.Interfaces;

namespace RealTimeApps.Applications.UseCases;

public class Messages
{
    private const int ReplyPreviewLength = 160;

    private readonly IMessageRepository _messageRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly IChannelRepository _channelRepository; 
    private readonly IServerRepository _serverRepository; 
    private readonly IServerMemberRepository _serverMemberRepository;
    private readonly IUnitOfWork _unitOfWork; 
    private readonly ICurrentUser _currentUser; 
    private readonly IRealtimeNotifier _realtimeNotifier; 
    private readonly IUserDirectory _userDirectory;

    public Messages(
        IMessageRepository messageRepository, 
        IMessageReactionRepository reactionRepository,
        IChannelRepository channelRepository, 
        IServerRepository serverRepository, 
        IServerMemberRepository serverMemberRepository, 
        IUnitOfWork unitOfWork, 
        ICurrentUser currentUser, 
        IRealtimeNotifier realtimeNotifier,
        IUserDirectory userDirectory)
    {
        _messageRepository = messageRepository; 
        _reactionRepository = reactionRepository;
        _channelRepository = channelRepository; 
        _serverRepository = serverRepository;
        _serverMemberRepository = serverMemberRepository; 
        _unitOfWork = unitOfWork; 
        _currentUser = currentUser; 
        _realtimeNotifier = realtimeNotifier;
        _userDirectory = userDirectory;
    }

    public async Task<MessageDto> SendAsync(
        int channelId, string content, CancellationToken ct,
        string? attachmentUrl = null, string? attachmentName = null,
        string? attachmentContentType = null, long? attachmentSize = null,
        int? replyToMessageId = null)
    {
        string userId = GetUserId(); 
        Channel channel = await GetChannelAsync(channelId, ct);
        await EnsureMemberAsync(userId, channel.ServerId, ct);

        Message? replyTo = null;

        if (replyToMessageId is int replyId)
        {
            replyTo = await _messageRepository.GetByIdAsync(replyId, ct)
                ?? throw new KeyNotFoundException("Reply message was not found");

            if (replyTo.ChannelId != channelId)
            {
                throw new ArgumentException(
                    "You can only reply to a message in the same channel",
                    nameof(replyToMessageId));
            }
        }

        DateTime currentTime = DateTime.UtcNow;

        Message message = new Message(
            content, channelId, userId, currentTime,
            attachmentUrl, attachmentName, attachmentContentType, attachmentSize,
            replyToMessageId);

        await _messageRepository.AddAsync(message, ct); 
        await _unitOfWork.SaveChangesAsync(ct);

        HashSet<string> profileIds = [userId];

        if (replyTo is not null)
        {
            profileIds.Add(replyTo.PersonalUserId);
        }

        IReadOnlyDictionary<string, UserProfile> profiles =
            await _userDirectory.GetProfilesAsync(profileIds.ToList(), ct);

        Dictionary<int, Message> replyMessages = [];

        if (replyTo is not null)
        {
            replyMessages[replyTo.Id] = replyTo;
        }

        MessageDto dto = ToDto(
            message,
            profiles,
            replyMessages,
            new Dictionary<int, List<MessageReaction>>(),
            userId);

        await _realtimeNotifier.MessageReceivedAsync(channelId, dto, ct);

        return dto;
    }

    public async Task<IReadOnlyList<MessageDto>> GetHistoryAsync(
        int channelId, int? beforeId, int limit, CancellationToken ct
    )
    {
        string userId = GetUserId(); 
        Channel channel = await GetChannelAsync(channelId, ct); 
        await EnsureMemberAsync(userId, channel.ServerId, ct);

        int take = Math.Clamp(limit, 1, 100); 
        IReadOnlyList<Message> messages = await _messageRepository 
            .GetPageAsync(channelId, beforeId, take, ct);

        List<int> messageIds = messages
            .Select(message => message.Id)
            .ToList();
        List<int> replyIds = messages
            .Where(message => message.ReplyToMessageId is not null)
            .Select(message => message.ReplyToMessageId!.Value)
            .Distinct()
            .ToList();

        IReadOnlyList<Message> replyMessages =
            await _messageRepository.GetByIdsAsync(replyIds, ct);
        Dictionary<int, Message> repliesById = replyMessages
            .ToDictionary(message => message.Id);

        IReadOnlyList<MessageReaction> reactions =
            await _reactionRepository.GetByMessageIdsAsync(messageIds, ct);
        Dictionary<int, List<MessageReaction>> reactionsByMessage = reactions
            .GroupBy(reaction => reaction.MessageId)
            .ToDictionary(group => group.Key, group => group.ToList());

        HashSet<string> profileIds = messages
            .Select(message => message.PersonalUserId)
            .Concat(replyMessages.Select(message => message.PersonalUserId))
            .ToHashSet();

        IReadOnlyDictionary<string, UserProfile> profiles =
            await _userDirectory.GetProfilesAsync(profileIds.ToList(), ct);

        return messages
            .Select(message => ToDto(
                message,
                profiles,
                repliesById,
                reactionsByMessage,
                userId))
            .ToList();
    }

    public async Task<ReactionResultDto> ToggleReactionAsync(
        int messageId, string emoji, CancellationToken ct)
    {
        string userId = GetUserId();
        Message message = await GetMessageAsync(messageId, ct);
        Channel channel = await GetChannelAsync(message.ChannelId, ct);
        await EnsureMemberAsync(userId, channel.ServerId, ct);

        string normalizedEmoji = (emoji ?? string.Empty).Trim();

        if (normalizedEmoji.Length == 0)
        {
            throw new ArgumentException("Reaction emoji is required", nameof(emoji));
        }

        if (normalizedEmoji.Length > 32)
        {
            throw new ArgumentException("Reaction emoji is too long", nameof(emoji));
        }

        MessageReaction? existing = await _reactionRepository.GetAsync(
            messageId, userId, normalizedEmoji, ct);
        bool isAdded = existing is null;

        if (existing is null)
        {
            await _reactionRepository.AddAsync(
                new MessageReaction(messageId, userId, normalizedEmoji), ct);
        }
        else
        {
            _reactionRepository.Remove(existing);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        IReadOnlyList<MessageReaction> reactions =
            await _reactionRepository.GetByMessageIdsAsync([messageId], ct);
        List<ReactionDto> summaries = BuildReactionSummaries(reactions);
        List<string> mine = reactions
            .Where(reaction => reaction.PersonalUserId == userId)
            .Select(reaction => reaction.Emoji)
            .Distinct()
            .ToList();

        await _realtimeNotifier.MessageReactionsChangedAsync(
            message.ChannelId,
            messageId,
            userId,
            normalizedEmoji,
            isAdded,
            summaries,
            ct);

        return new ReactionResultDto(messageId, summaries, mine);
    }

    public async Task EditAsync(int messageId, string content, CancellationToken ct)
    {
        string userId = GetUserId(); 
        Message message = await GetMessageAsync(messageId, ct);

        if (message.PersonalUserId != userId)
            throw new UnauthorizedAccessException("You can only edit your message");

        message.Edit(content); 
        await _unitOfWork.SaveChangesAsync(ct); 
        await _realtimeNotifier.MessageEditedAsync(message.ChannelId, message.Id, ct);
    }

    public async Task<string?> DeleteAsync(int messageId, CancellationToken ct)
    {
        string userId = GetUserId(); 
        Message message = await GetMessageAsync(messageId, ct); 
        Channel channel = await GetChannelAsync(message.ChannelId, ct);
        Server server = await GetServerAsync(channel.ServerId, ct);

        bool isAuthor = message.PersonalUserId == userId; 
        bool isServerOwner = server.OwnerId == userId; 

        if (!isAuthor && !isServerOwner)
            throw new UnauthorizedAccessException("You cannot delete the message");

        string? attachmentUrl = message.AttachmentUrl;
        
        _messageRepository.Remove(message); 
        await _unitOfWork.SaveChangesAsync(ct); 
        await _realtimeNotifier.MessageDeletedAsync(channel.Id, messageId, ct);

        return attachmentUrl;
    }

    private async Task<Message> GetMessageAsync(int messageId, CancellationToken ct)
    {
        return await _messageRepository.GetByIdAsync(messageId, ct)
            ?? throw new KeyNotFoundException("Message was not found");
    }

    private async Task<Server> GetServerAsync(Guid serverId, CancellationToken ct)
    {
        return await _serverRepository.GetByIdAsync(serverId, ct)
            ?? throw new KeyNotFoundException("Server was not found");
    }

    private async Task EnsureMemberAsync(string userId, Guid serverId, CancellationToken ct)
    {
        if (!await _serverMemberRepository.IsMemberAsync(userId, serverId, ct))
            throw new UnauthorizedAccessException("User are not the member of this channel");
    }

    private async Task<Channel> GetChannelAsync(int channelId, CancellationToken ct)
    {
        return await _channelRepository.GetByIdAsync(channelId, ct)
            ?? throw new KeyNotFoundException("Channel was not found");
    }

    private string GetUserId()
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedAccessException("Authentication is required"); 
        
        return _currentUser.UserId;
    }

    private static MessageDto ToDto(
        Message message,
        IReadOnlyDictionary<string, UserProfile> profiles,
        IReadOnlyDictionary<int, Message> replyMessages,
        IReadOnlyDictionary<int, List<MessageReaction>> reactionsByMessage,
        string currentUserId)
    {
        profiles.TryGetValue(message.PersonalUserId, out UserProfile? profile);

        ReplyPreviewDto? replyTo = null;

        if (message.ReplyToMessageId is int replyId)
        {
            if (replyMessages.TryGetValue(replyId, out Message? replyMessage))
            {
                profiles.TryGetValue(
                    replyMessage.PersonalUserId,
                    out UserProfile? replyProfile);

                replyTo = new ReplyPreviewDto(
                    replyId,
                    replyMessage.PersonalUserId,
                    replyProfile?.DisplayName ?? "Unknown",
                    BuildPreviewContent(replyMessage));
            }
            else
            {
                replyTo = new ReplyPreviewDto(
                    replyId,
                    string.Empty,
                    "Unknown",
                    "message deleted");
            }
        }

        List<ReactionDto> reactions = [];
        List<string> myReactions = [];

        if (reactionsByMessage.TryGetValue(
                message.Id,
                out List<MessageReaction>? messageReactions))
        {
            reactions = BuildReactionSummaries(messageReactions);
            myReactions = messageReactions
                .Where(reaction => reaction.PersonalUserId == currentUserId)
                .Select(reaction => reaction.Emoji)
                .Distinct()
                .ToList();
        }

        return new MessageDto(
            message.Id, message.ChannelId, message.PersonalUserId,
            profile?.DisplayName ?? "Unknown",
            message.MessageContent, message.CreatedAt, message.EditedAt,
            profile?.AvatarUrl, message.AttachmentUrl, message.AttachmentName,
            message.AttachmentContentType, message.AttachmentSize,
            replyTo, reactions, myReactions);
    }

    private static List<ReactionDto> BuildReactionSummaries(
        IReadOnlyList<MessageReaction> reactions) =>
        reactions
            .GroupBy(reaction => reaction.Emoji)
            .Select(group => new ReactionDto(group.Key, group.Count()))
            .OrderByDescending(reaction => reaction.Count)
            .ThenBy(reaction => reaction.Emoji, StringComparer.Ordinal)
            .ToList();

    private static string BuildPreviewContent(Message message)
    {
        string content = message.MessageContent
            .ReplaceLineEndings(" ")
            .Trim();

        if (content.Length == 0 &&
            !string.IsNullOrWhiteSpace(message.AttachmentName))
        {
            content = "[file] " + message.AttachmentName;
        }

        if (content.Length == 0)
        {
            content = "(no content)";
        }

        return content.Length <= ReplyPreviewLength
            ? content
            : content[..(ReplyPreviewLength - 1)] + "…";
    }
}
