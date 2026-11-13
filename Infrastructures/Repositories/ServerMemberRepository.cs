using Microsoft.EntityFrameworkCore;
using Qwek.Domains.Entities;
using Qwek.Domains.Interfaces;
using Qwek.Infrastructures.Persistence;

namespace Qwek.Infrastructures.Repositories;

public class ServerMemberRepository : IServerMemberRepository
{
    private readonly AppDbContext _db;

    public ServerMemberRepository(AppDbContext db) => _db = db;

    public Task<bool> IsMemberAsync(string userId, Guid serverId, CancellationToken ct) => 
        _db.ServerMembers.AnyAsync( 
            m => m.PersonalUserId == userId && m.ServerId == serverId, ct
        );

    public async Task<IReadOnlyList<ServerMember>> GetMemberAsync(
        Guid serverId, CancellationToken ct
    )
    {
        return await _db.ServerMembers
            .Where(m => m.ServerId == serverId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ServerMember member, CancellationToken ct) =>
        await _db.ServerMembers.AddAsync(member, ct);

    public async Task RemoveAsync(string userId, Guid serverId, CancellationToken ct)
    {
        ServerMember? member = await _db.ServerMembers.FirstOrDefaultAsync( 
            m => m.PersonalUserId == userId && m.ServerId == serverId, ct
        );

        if (member is not null) 
            _db.ServerMembers.Remove(member);
    }
}
