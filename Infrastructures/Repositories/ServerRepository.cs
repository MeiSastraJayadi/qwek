using Microsoft.EntityFrameworkCore;
using RealTimeApps.Domains.Entities;
using RealTimeApps.Domains.Interfaces;
using RealTimeApps.Infrastructures.Persistence;

namespace RealTimeApps.Infrastructures.Repositories;

public class ServerRepository : IServerRepository
{
    private readonly AppDbContext _db;
    public ServerRepository(AppDbContext db) => _db = db;

    public Task<Server?> GetByIdAsync(Guid id, CancellationToken ct) => 
        _db.Servers.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Server>> GetForUserAsync(string userId, CancellationToken ct)
    {
        return await _db.ServerMembers
            .Where(m => m.PersonalUserId == userId)
            .Join(_db.Servers, m => m.ServerId, s => s.Id, (_, s) => s)
            .ToListAsync(ct);
    }

    public Task<bool> ExistAsync(Guid id, CancellationToken ct) => 
        _db.Servers.AnyAsync( s => s.Id == id, ct);

    public async Task AddAsync(Server server, CancellationToken ct) => 
        await _db.Servers.AddAsync(server, ct);
    
    public void Remove(Server server) => _db.Servers.Remove(server);
}
