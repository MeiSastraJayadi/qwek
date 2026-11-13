using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Ports;

namespace Qwek.Infrastructures.Security;

public sealed class ClamAvFileScanner : IFileScanner
{
    private readonly AntivirusOptions _options;

    public ClamAvFileScanner(IOptions<AntivirusOptions> options) =>
        _options = options.Value;

    public async Task<bool> IsCleanAsync(Stream content, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            return true;
        }

        using CancellationTokenSource timeout =
            CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        using TcpClient client = new();
        await client.ConnectAsync(_options.Host, _options.Port, timeout.Token);

        await using NetworkStream stream = client.GetStream();
        byte[] command = Encoding.ASCII.GetBytes("zINSTREAM\0");
        await stream.WriteAsync(command, timeout.Token);

        byte[] buffer = new byte[81920];
        int read;

        while ((read = await content.ReadAsync(buffer, timeout.Token)) > 0)
        {
            byte[] length = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, read);

            await stream.WriteAsync(length, timeout.Token);
            await stream.WriteAsync(
                buffer.AsMemory(0, read), timeout.Token);
        }

        await stream.WriteAsync(new byte[4], timeout.Token);

        string response = await ReadResponseAsync(stream, timeout.Token);

        if (response.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (response.Contains("OK", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        throw new InvalidOperationException(
            "The file scanner returned an unexpected response.");
    }

    private static async Task<string> ReadResponseAsync(
        Stream stream, CancellationToken ct)
    {
        byte[] buffer = new byte[256];
        int total = 0;

        while (total < buffer.Length)
        {
            int read = await stream.ReadAsync(
                buffer.AsMemory(total), ct);

            if (read == 0)
            {
                break;
            }

            int terminator = Array.IndexOf(buffer, (byte)0, total, read);

            if (terminator >= 0)
            {
                total = terminator;
                break;
            }

            total += read;
        }

        return Encoding.ASCII.GetString(buffer, 0, total);
    }
}
