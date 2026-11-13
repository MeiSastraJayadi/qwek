namespace Qwek.Applications.Ports;

public interface IFileScanner
{
    Task<bool> IsCleanAsync(Stream content, CancellationToken ct);
}
