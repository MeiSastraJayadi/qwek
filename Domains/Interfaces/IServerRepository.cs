using RealTimeApps.Domains.Entities;

namespace RealTimeApps.Domains.Interfaces;

public interface IServerRepository
{
    Task<Server?> GetByIdAsync(Guid id, CancellationToken ct); 
    Task<IReadOnlyList<Server>> GetForUserAsync(string userId, CancellationToken ct); 
    Task<bool> ExistAsync(Guid id, CancellationToken ct);
    Task AddAsync(Server server, CancellationToken ct);
    void Remove(Server server);
}
