namespace Qwek.Applications.Ports;

public interface IStorageQuota
{
    Task<StorageQuotaReservation> ReserveAsync(
        string userId, long length, long replaceBytes,
        CancellationToken ct);

    Task CompleteAsync(
        StorageQuotaReservation reservation, CancellationToken ct);

    Task ReleaseAsync(
        StorageQuotaReservation reservation, CancellationToken ct);
}

public sealed record StorageQuotaReservation(Guid Id, long Bytes);
