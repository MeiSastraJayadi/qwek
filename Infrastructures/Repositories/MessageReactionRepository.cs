using Microsoft.EntityFrameworkCore;
using RealTimeApps.Domains.Entities;
using RealTimeApps.Domains.Interfaces;
using RealTimeApps.Infrastructures.Persistence;

namespace RealTimeApps.Infrastructures.Repositories;

public class MessageReactionRepository : IMessageReactionRepository
{
    private readonly AppDbContext _db;

    public MessageReactionRepository(AppDbContext db) => _db = db;

    public Task<MessageReaction?> GetAsync(
        int messageId, string userId, string emoji, CancellationToken ct) =>
        _db.MessageReactions.FirstOrDefaultAsync(
            reaction =>
                reaction.MessageId == messageId &&
                reaction.PersonalUserId == userId &&
                reaction.Emoji == emoji,
            ct);

    public async Task<IReadOnlyList<MessageReaction>> GetByMessageIdsAsync(
        IReadOnlyCollection<int> messageIds, CancellationToken ct)
    {
        if (messageIds.Count == 0)
        {
            return [];
        }

        return await _db.MessageReactions
            .Where(reaction => messageIds.Contains(reaction.MessageId))
            .OrderBy(reaction => reaction.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(MessageReaction reaction, CancellationToken ct) =>
        await _db.MessageReactions.AddAsync(reaction, ct);

    public void Remove(MessageReaction reaction) =>
        _db.MessageReactions.Remove(reaction);
}
