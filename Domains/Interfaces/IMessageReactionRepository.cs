using Qwek.Domains.Entities;

namespace Qwek.Domains.Interfaces;

public interface IMessageReactionRepository
{
    Task<MessageReaction?> GetAsync(
        int messageId, string userId, string emoji, CancellationToken ct);

    Task<IReadOnlyList<MessageReaction>> GetByMessageIdsAsync(
        IReadOnlyCollection<int> messageIds, CancellationToken ct);

    Task AddAsync(MessageReaction reaction, CancellationToken ct);
    void Remove(MessageReaction reaction);
}
