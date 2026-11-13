namespace Qwek.Interfaces.Web;

public static class FileSignatureValidator
{
    private const int HeaderSize = 16;

    public static async Task<bool> MatchesExtensionAsync(
        IFormFile file, string extension, CancellationToken ct)
    {
        byte[] header = new byte[HeaderSize];
        int totalRead = 0;

        await using Stream stream = file.OpenReadStream();

        while (totalRead < header.Length)
        {
            int read = await stream.ReadAsync(
                header.AsMemory(totalRead), ct);

            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return Matches(extension, header.AsSpan(0, totalRead));
    }

    private static bool Matches(string extension, ReadOnlySpan<byte> header)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
            ".gif" => header.StartsWith("GIF87a"u8) ||
                header.StartsWith("GIF89a"u8),
            ".webp" => header.Length >= 12 &&
                header[..4].SequenceEqual("RIFF"u8) &&
                header[8..12].SequenceEqual("WEBP"u8),
            ".pdf" => header.StartsWith("%PDF-"u8),
            ".mp4" or ".mov" => header.Length >= 12 &&
                header[4..8].SequenceEqual("ftyp"u8),
            ".webm" => header.StartsWith(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }),
            ".mp3" => header.StartsWith("ID3"u8) ||
                (header.Length >= 2 &&
                    header[0] == 0xFF &&
                    (header[1] & 0xE0) == 0xE0),
            ".wav" => header.Length >= 12 &&
                header[..4].SequenceEqual("RIFF"u8) &&
                header[8..12].SequenceEqual("WAVE"u8),
            ".ogg" => header.StartsWith("OggS"u8),
            ".txt" or ".md" or ".csv" => IsPlainText(header),
            _ => false
        };
    }

    private static bool IsPlainText(ReadOnlySpan<byte> header)
    {
        if (header.IsEmpty)
        {
            return false;
        }

        foreach (byte value in header)
        {
            if (value == 0)
            {
                return false;
            }

            if (value < 0x20 && value is not (0x09 or 0x0A or 0x0D))
            {
                return false;
            }
        }

        return true;
    }
}
