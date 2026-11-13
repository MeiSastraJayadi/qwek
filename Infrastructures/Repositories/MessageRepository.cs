using Microsoft.EntityFrameworkCore;
using Qwek.Domains.Entities;
using Qwek.Domains.Interfaces;
using Qwek.Infrastructures.Persistence;

namespace Qwek.Infrastructures.Repositories;

public class MessageRepository : IMessageRepository
{

    private readonly AppDbContext _db; 
    public MessageRepository(AppDbContext db) => _db = db;

    public Task<Message?> GetByIdAsync(int id, CancellationToken ct) => 
        _db.Messages.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<Message>> GetByIdsAsync(
        IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await _db.Messages
            .Where(message => ids.Contains(message.Id))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Message>> GetPageAsync(
        int channelId, int? beforeId, int limit, CancellationToken ct
    )
    {
        IQueryable<Message> query = _db.Messages
            .Where(m => m.ChannelId == channelId); 

        if (beforeId is not null)
            query = query.Where(m => m.Id < beforeId.Value);
        
        return await query
            .OrderByDescending(m => m.Id)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Message message, CancellationToken ct) => 
        await _db.Messages.AddAsync(message, ct); 
    
    public void Remove(Message message) => _db.Messages.Remove(message);

}
