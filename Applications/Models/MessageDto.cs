namespace Qwek.Applications.Models;

public record class ReplyPreviewDto(
    int MessageId,
    string UserId,
    string DisplayName,
    string Content
);

public record class ReactionDto(string Emoji, int Count);

public record class ReactionResultDto(
    int MessageId,
    IReadOnlyList<ReactionDto> Reactions,
    IReadOnlyList<string> MyReactions
);

public record class MessageDto(
    int Id, int ChannelId, string UserId, string DisplayName, string Content,
    DateTime CreatedAt, DateTime? EditedAt, string? AvatarUrl = null,
    string? AttachmentUrl = null, string? AttachmentName = null,
    string? AttachmentContentType = null, long? AttachmentSize = null,
    ReplyPreviewDto? ReplyTo = null,
    IReadOnlyList<ReactionDto>? Reactions = null,
    IReadOnlyList<string>? MyReactions = null
);
