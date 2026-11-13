using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qwek.Applications.Commons;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Ports;
using Qwek.Domains.Entities;

namespace Qwek.Infrastructures.Persistence;

public class StorageQuota : IStorageQuota
{
    private const long LockKey = 847326591;
    private static readonly TimeSpan ReservationLifetime =
        TimeSpan.FromMinutes(15);

    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly StorageOptions _options;

    public StorageQuota(
        AppDbContext db,
        IFileStorage storage,
        IOptions<StorageOptions> options)
    {
        _db = db;
        _storage = storage;
        _options = options.Value;
    }

    public async Task<StorageQuotaReservation> ReserveAsync(
        string userId, long length, long replaceBytes,
        CancellationToken ct)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(ct);

        await _db.Database.ExecuteSqlRawAsync(
            $"SELECT pg_advisory_xact_lock({LockKey})", ct);

        DateTime now = DateTime.UtcNow;

        await _db.StorageReservations
            .Where(reservation => reservation.ExpiresAt <= now)
            .ExecuteDeleteAsync(ct);

        long used = await _storage.GetUsedBytesAsync(ct);

        long reservedTotal = await _db.StorageReservations
            .SumAsync(reservation => (long?)reservation.Bytes, ct) ?? 0;
        long reservedUser = await _db.StorageReservations
            .Where(reservation => reservation.UserId == userId)
            .SumAsync(reservation => (long?)reservation.Bytes, ct) ?? 0;

        long attachments = await _db.Messages
            .Where(message => message.PersonalUserId == userId &&
                message.AttachmentSize != null)
            .SumAsync(message => message.AttachmentSize, ct) ?? 0;
        long avatarSize = await _db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.AvatarSize)
            .FirstOrDefaultAsync(ct);

        if (used + reservedTotal + length - replaceBytes >
            _options.MaxTotalBytes)
        {
            throw new StorageQuotaExceededException(_options.MaxTotalBytes);
        }

        if (attachments + avatarSize - replaceBytes + reservedUser + length >
            _options.MaxUserBytes)
        {
            throw new StorageQuotaExceededException(
                _options.MaxUserBytes,
                "Your upload quota has been reached.");
        }

        StorageReservation stored = new(
            userId, length, now.Add(ReservationLifetime));

        await _db.StorageReservations.AddAsync(stored, ct);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new StorageQuotaReservation(stored.Id, length);
    }

    public Task CompleteAsync(
        StorageQuotaReservation reservation, CancellationToken ct) =>
        RemoveAsync(reservation, ct);

    public Task ReleaseAsync(
        StorageQuotaReservation reservation, CancellationToken ct) =>
        RemoveAsync(reservation, ct);

    private Task RemoveAsync(
        StorageQuotaReservation reservation, CancellationToken ct) =>
        _db.StorageReservations
            .Where(stored => stored.Id == reservation.Id)
            .ExecuteDeleteAsync(ct);
}
