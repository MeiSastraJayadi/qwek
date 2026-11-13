using Microsoft.AspNetCore.SignalR;
using Qwek.Applications.Models;
using Qwek.Applications.Ports;

namespace Qwek.Interfaces.RealTime;

public class RealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hub; 

    public RealtimeNotifier(IHubContext<ChatHub> hub) => _hub = hub;

    public Task MessageReceivedAsync(int channelId, object message, CancellationToken ct) => 
        _hub.Clients.Group($"channel:{channelId}")
            .SendAsync("ReceiveMessage", message, ct);

    public Task MessageEditedAsync(int channelId, int messageId, CancellationToken ct) => 
        _hub.Clients.Group($"channel:{channelId}")
            .SendAsync("MessageEdited", new { channelId, messageId }, ct);

    
    public Task MessageDeletedAsync(int channelId, int messageId, CancellationToken ct) =>
        _hub.Clients.Group($"channel:{channelId}")
            .SendAsync("MessageDeleted", new { channelId, messageId }, ct);

    public Task MessageReactionsChangedAsync(
        int channelId, int messageId, string userId, string emoji,
        bool isAdded, IReadOnlyList<ReactionDto> reactions,
        CancellationToken ct) =>
        _hub.Clients.Group($"channel:{channelId}")
            .SendAsync("MessageReactionsChanged", new
            {
                channelId,
                messageId,
                userId,
                emoji,
                isAdded,
                reactions
            }, ct);
    
    public Task MemberJoinedServerAsync(Guid serverId, string userId, CancellationToken ct) => 
        _hub.Clients.Group($"server:{serverId}")
            .SendAsync("MemberJoined", new { serverId, userId }, ct);

     public Task MemberLeftServerAsync(Guid serverId, string userId, CancellationToken ct) => 
        _hub.Clients.Group($"server:{serverId}")
            .SendAsync("MemberLeft", new { serverId, userId }, ct);
    

}
