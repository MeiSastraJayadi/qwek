namespace Qwek.Applications.Commons.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";
    public bool Private { get; set; }
    public string PrivateBasePath { get; set; } = "/api/files";
    public long MaxTotalBytes { get; set; } = 9_000_000_000;
    public long MaxUserBytes { get; set; } = 500 * 1024 * 1024;
    public long MaxAttachmentBytes { get; set; } = 25 * 1024 * 1024;
    public long MaxAvatarBytes { get; set; } = 5 * 1024 * 1024;
    public LocalStorageOptions Local { get; set; } = new();
    public S3StorageOptions S3 { get; set; } = new();
}

public class LocalStorageOptions
{
    public string Path { get; set; } = "uploads";
    public string PublicBasePath { get; set; } = "/uploads";
}

public class S3StorageOptions
{
    public string Bucket { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = "auto";
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
}
