namespace Qwek.Domains.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsActive =>
        RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    private RefreshToken()
    {
    }

    public RefreshToken(string userId, string tokenHash, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    public void Revoke() => RevokedAt = DateTime.UtcNow;
}
