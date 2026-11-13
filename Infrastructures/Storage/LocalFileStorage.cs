using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Qwek.Applications.Commons;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Ports;

namespace Qwek.Infrastructures.Storage;

public sealed class LocalFileStorage : FileStorageBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypes =
        CreateContentTypes();

    private readonly string _root;
    private readonly string _publicBasePath;
    private readonly string _privateBasePath;

    public LocalFileStorage(
        IOptions<StorageOptions> options, IHostEnvironment environment)
        : base(options)
    {
        _root = Path.GetFullPath(
            Path.Combine(environment.ContentRootPath, Options.Local.Path));
        _publicBasePath = "/" + Options.Local.PublicBasePath.Trim('/');
        _privateBasePath = "/" + Options.PrivateBasePath.Trim('/');

        Directory.CreateDirectory(_root);
    }

    protected override async Task<string> SaveCoreAsync(
        string folder, string storedName, long length, string contentType,
        Stream content, CancellationToken ct)
    {
        string directory = Path.Combine(_root, folder);
        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, storedName);

        try
        {
            await using FileStream stream = File.Create(path);
            await content.CopyToAsync(stream, ct);
        }
        catch
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
            }

            throw;
        }

        string basePath = Options.Private ? _privateBasePath : _publicBasePath;
        return $"{basePath}/{folder}/{storedName}";
    }

    protected override Task<StoredFile?> OpenReadCoreAsync(
        string url, CancellationToken ct)
    {
        string? relative = GetRelativePath(url);

        if (relative is null)
        {
            return Task.FromResult<StoredFile?>(null);
        }

        string path = Path.GetFullPath(Path.Combine(_root, relative));

        if (!IsUnderRoot(path) || !File.Exists(path))
        {
            return Task.FromResult<StoredFile?>(null);
        }

        ct.ThrowIfCancellationRequested();

        FileStream stream = File.OpenRead(path);
        string contentType = ContentTypes.TryGetContentType(
            path, out string? resolved)
            ? resolved
            : "application/octet-stream";

        return Task.FromResult<StoredFile?>(
            new StoredFile(stream, contentType, stream.Length));
    }

    protected override Task DeleteCoreAsync(string? url, CancellationToken ct)
    {
        string? relative = GetRelativePath(url);

        if (relative is null)
        {
            return Task.CompletedTask;
        }

        string path = Path.GetFullPath(Path.Combine(_root, relative));

        if (!IsUnderRoot(path))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    protected override Task<long> GetUsedBytesCoreAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_root))
        {
            return Task.FromResult(0L);
        }

        long total = 0;

        foreach (string file in Directory.EnumerateFiles(
            _root, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            total += new FileInfo(file).Length;
        }

        return Task.FromResult(total);
    }

    public override bool IsManagedUrl(string? url, string folder) =>
        StoragePaths.IsManagedRelativePath(GetRelativePath(url), folder);

    private string? GetRelativePath(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        string privatePrefix = _privateBasePath + "/";
        string publicPrefix = _publicBasePath + "/";
        string? prefix = null;

        if (url.StartsWith(privatePrefix, StringComparison.OrdinalIgnoreCase))
        {
            prefix = privatePrefix;
        }
        else if (url.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            prefix = publicPrefix;
        }

        if (prefix is null)
        {
            return null;
        }

        return Uri.UnescapeDataString(
            url[prefix.Length..].Split('?')[0]);
    }

    private bool IsUnderRoot(string path)
    {
        string rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        return path.StartsWith(rootPrefix, StringComparison.Ordinal);
    }

    private static FileExtensionContentTypeProvider CreateContentTypes()
    {
        FileExtensionContentTypeProvider provider = new();
        provider.Mappings[".md"] = "text/markdown";
        provider.Mappings[".webm"] = "video/webm";
        provider.Mappings[".webp"] = "image/webp";
        provider.Mappings[".mov"] = "video/quicktime";
        provider.Mappings[".ogg"] = "audio/ogg";
        provider.Mappings[".wav"] = "audio/wav";
        provider.Mappings[".mp3"] = "audio/mpeg";
        return provider;
    }
}
