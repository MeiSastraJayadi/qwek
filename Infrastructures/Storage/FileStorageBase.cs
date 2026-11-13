using Microsoft.Extensions.Options;
using Qwek.Applications.Commons;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Ports;

namespace Qwek.Infrastructures.Storage;

public abstract class FileStorageBase : IFileStorage
{
    private readonly SemaphoreSlim _quotaLock = new(1, 1);
    private long _reservedBytes;

    protected FileStorageBase(IOptions<StorageOptions> options) =>
        Options = options.Value;

    protected StorageOptions Options { get; }

    public async Task<string> SaveAsync(
        string folder, string fileName, long length, string contentType,
        Stream content, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(folder) ||
            folder.Contains('/') ||
            folder.Contains('\\') ||
            folder.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Storage folder is invalid", nameof(folder));
        }

        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension.Length is 0 or > 16 ||
            extension.Contains('/') ||
            extension.Contains('\\') ||
            extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("File extension is invalid", nameof(fileName));
        }

        string storedName = $"{Guid.NewGuid():N}{extension}";

        await ReserveAsync(length, ct);

        try
        {
            return await SaveCoreAsync(
                folder, storedName, length, contentType, content, ct);
        }
        finally
        {
            Interlocked.Add(ref _reservedBytes, -length);
        }
    }

    public Task<StoredFile?> OpenReadAsync(string url, CancellationToken ct) =>
        OpenReadCoreAsync(url, ct);

    public Task DeleteAsync(string? url, CancellationToken ct) =>
        DeleteCoreAsync(url, ct);

    private async Task ReserveAsync(long length, CancellationToken ct)
    {
        await _quotaLock.WaitAsync(ct);

        try
        {
            long used = await GetUsedBytesCoreAsync(ct);
            long reserved = Interlocked.Read(ref _reservedBytes);

            if (used + reserved + length > Options.MaxTotalBytes)
            {
                throw new StorageQuotaExceededException(Options.MaxTotalBytes);
            }

            Interlocked.Add(ref _reservedBytes, length);
        }
        finally
        {
            _quotaLock.Release();
        }
    }

    protected abstract Task<string> SaveCoreAsync(
        string folder, string storedName, long length, string contentType,
        Stream content, CancellationToken ct);

    protected abstract Task<StoredFile?> OpenReadCoreAsync(
        string url, CancellationToken ct);

    protected abstract Task DeleteCoreAsync(string? url, CancellationToken ct);

    protected abstract Task<long> GetUsedBytesCoreAsync(CancellationToken ct);
}
