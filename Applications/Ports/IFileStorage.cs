namespace Qwek.Applications.Ports;

public interface IFileStorage
{
    Task<string> SaveAsync(
        string folder, string fileName, long length, string contentType,
        Stream content, CancellationToken ct);

    Task<StoredFile?> OpenReadAsync(string url, CancellationToken ct);

    Task DeleteAsync(string? url, CancellationToken ct);
}

public sealed record StoredFile(Stream Stream, string ContentType, long Length);
