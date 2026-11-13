using Microsoft.EntityFrameworkCore;
using Qwek.Domains.Entities;
using Qwek.Domains.Interfaces;
using Qwek.Infrastructures.Persistence;

namespace Qwek.Infrastructures.Repositories;

public class ChannelRepository : IChannelRepository
{
    private readonly AppDbContext _db; 

    public ChannelRepository(AppDbContext db) => _db = db; 

    public Task<Channel?> GetByIdAsync(int id, CancellationToken ct) => 
        _db.Channels.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Channel>> GetByServerAsync(
        Guid serverId, CancellationToken ct
    )
    {
        return await _db.Channels
            .Where(c => c.ServerId == serverId)
            .OrderBy(c => c.Id)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Channel channel, CancellationToken ct) => 
        await _db.Channels.AddAsync(channel, ct);

    
    public void Remove(Channel channel) => _db.Channels.Remove(channel);
}
