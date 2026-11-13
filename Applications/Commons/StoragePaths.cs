namespace Qwek.Applications.Commons;

public static class StoragePaths
{
    public static bool IsSafeFileName(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        fileName is not "." and not ".." &&
        fileName == Path.GetFileName(fileName) &&
        !fileName.Contains('\\') &&
        fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    public static bool IsManagedRelativePath(
        string? relativePath, string folder)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        string[] segments = relativePath.Split('/');

        return segments.Length == 2 &&
            string.Equals(segments[0], folder, StringComparison.Ordinal) &&
            IsSafeFileName(segments[1]);
    }
}
