using RealTimeApps.Domains.Entities;

namespace RealTimeApps.Domains.Interfaces;

public interface IServerMemberRepository
{
    Task<bool> IsMemberAsync(string userId, Guid serverId, CancellationToken ct);
    Task<IReadOnlyList<ServerMember>> GetMemberAsync(Guid serverId, CancellationToken ct);
    Task AddAsync(ServerMember member, CancellationToken ct); 
    Task RemoveAsync(string userId, Guid serverId, CancellationToken ct); 
}
