using RealTimeApps.Applications.Models;

namespace RealTimeApps.Applications.Ports;

public interface IRealtimeNotifier
{
    Task MessageReceivedAsync(int channelId, object message, CancellationToken ct);
    Task MessageEditedAsync(int channelId, int messageId, CancellationToken ct); 
    Task MessageDeletedAsync(int channelId, int messageId, CancellationToken ct);
    Task MessageReactionsChangedAsync(
        int channelId, int messageId, string userId, string emoji,
        bool isAdded, IReadOnlyList<ReactionDto> reactions,
        CancellationToken ct);
    Task MemberJoinedServerAsync(Guid serverId, string userId, CancellationToken ct); 
    Task MemberLeftServerAsync(Guid serverId, string userId, CancellationToken ct);
}
