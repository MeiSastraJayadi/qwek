using Qwek.Domains.Entities;

namespace Qwek.Domains.Interfaces;

public interface IChannelRepository
{
    Task<Channel?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Channel>> GetByServerAsync(Guid serverId, CancellationToken ct);
    Task AddAsync(Channel channel, CancellationToken ct);
    void Remove(Channel channel);
}
