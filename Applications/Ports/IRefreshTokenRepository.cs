using Qwek.Domains.Entities;

namespace Qwek.Applications.Ports;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task<bool> TryRevokeAsync(RefreshToken token, CancellationToken ct);
    Task RevokeAllForUserAsync(string userId, CancellationToken ct);
}
