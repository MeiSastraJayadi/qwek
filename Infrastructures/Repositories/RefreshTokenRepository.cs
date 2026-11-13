using Microsoft.EntityFrameworkCore;
using Qwek.Applications.Ports;
using Qwek.Domains.Entities;
using Qwek.Infrastructures.Persistence;

namespace Qwek.Infrastructures.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        await _db.RefreshTokens.AddAsync(token, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<RefreshToken?> GetByHashAsync(
        string tokenHash, CancellationToken ct) =>
        _db.RefreshTokens.FirstOrDefaultAsync(
            token => token.TokenHash == tokenHash, ct);

    public async Task<bool> TryRevokeAsync(
        RefreshToken token, CancellationToken ct)
    {
        int affected = await _db.RefreshTokens
            .Where(stored => stored.Id == token.Id && stored.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    stored => stored.RevokedAt, DateTime.UtcNow),
                ct);

        return affected > 0;
    }

    public Task RevokeAllForUserAsync(string userId, CancellationToken ct) =>
        _db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    token => token.RevokedAt, DateTime.UtcNow),
                ct);
}
