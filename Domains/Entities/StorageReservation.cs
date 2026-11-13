namespace Qwek.Domains.Entities;

public class StorageReservation
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public long Bytes { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private StorageReservation()
    {
    }

    public StorageReservation(string userId, long bytes, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Bytes = bytes;
        ExpiresAt = expiresAt;
    }
}
