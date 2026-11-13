using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Qwek.Applications.Commons;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Ports;

namespace Qwek.Infrastructures.Storage;

public sealed class S3FileStorage : FileStorageBase, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly string _publicBaseUrl;
    private readonly string _privateBasePath;

    public S3FileStorage(IOptions<StorageOptions> options) : base(options)
    {
        S3StorageOptions s3 = Options.S3;

        if (string.IsNullOrWhiteSpace(s3.Bucket) ||
            string.IsNullOrWhiteSpace(s3.Endpoint) ||
            string.IsNullOrWhiteSpace(s3.AccessKeyId) ||
            string.IsNullOrWhiteSpace(s3.SecretAccessKey))
        {
            throw new InvalidOperationException(
                "Storage:S3 configuration is incomplete.");
        }

        if (!Options.Private && string.IsNullOrWhiteSpace(s3.PublicBaseUrl))
        {
            throw new InvalidOperationException(
                "Storage:S3:PublicBaseUrl is required unless Storage:Private is true.");
        }

        AmazonS3Config config = new()
        {
            ServiceURL = s3.Endpoint,
            AuthenticationRegion = s3.Region,
            ForcePathStyle = true,
            RequestChecksumCalculation =
                RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation =
                ResponseChecksumValidation.WHEN_REQUIRED
        };

        _client = new AmazonS3Client(
            new BasicAWSCredentials(s3.AccessKeyId, s3.SecretAccessKey),
            config);
        _bucket = s3.Bucket;
        _publicBaseUrl = s3.PublicBaseUrl.TrimEnd('/');
        _privateBasePath = "/" + Options.PrivateBasePath.Trim('/');
    }

    protected override async Task<string> SaveCoreAsync(
        string folder, string storedName, long length, string contentType,
        Stream content, CancellationToken ct)
    {
        string key = $"{folder}/{storedName}";

        PutObjectRequest request = new()
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType
        };

        request.Headers.ContentLength = length;

        await _client.PutObjectAsync(request, ct);

        string basePath = Options.Private ? _privateBasePath : _publicBaseUrl;
        return $"{basePath}/{key}";
    }

    protected override async Task<StoredFile?> OpenReadCoreAsync(
        string url, CancellationToken ct)
    {
        string? key = GetKey(url);

        if (key is null)
        {
            return null;
        }

        try
        {
            GetObjectResponse response =
                await _client.GetObjectAsync(_bucket, key, ct);

            return new StoredFile(
                response.ResponseStream,
                response.Headers.ContentType ?? "application/octet-stream",
                response.Headers.ContentLength);
        }
        catch (AmazonS3Exception exception)
            when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    protected override async Task DeleteCoreAsync(
        string? url, CancellationToken ct)
    {
        string? key = GetKey(url);

        if (key is null)
        {
            return;
        }

        await _client.DeleteObjectAsync(_bucket, key, ct);
    }

    protected override async Task<long> GetUsedBytesCoreAsync(
        CancellationToken ct)
    {
        long total = 0;
        string? continuationToken = null;

        do
        {
            ListObjectsV2Request request = new()
            {
                BucketName = _bucket,
                ContinuationToken = continuationToken
            };

            ListObjectsV2Response response =
                await _client.ListObjectsV2Async(request, ct);

            total += response.S3Objects.Sum(item => item.Size ?? 0);

            continuationToken = response.IsTruncated == true
                ? response.NextContinuationToken
                : null;
        }
        while (!string.IsNullOrEmpty(continuationToken));

        return total;
    }

    public override bool IsManagedUrl(string? url, string folder) =>
        StoragePaths.IsManagedRelativePath(GetKey(url), folder);

    private string? GetKey(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        string privatePrefix = _privateBasePath + "/";
        string? prefix = null;

        if (url.StartsWith(privatePrefix, StringComparison.OrdinalIgnoreCase))
        {
            prefix = privatePrefix;
        }
        else if (!string.IsNullOrWhiteSpace(_publicBaseUrl))
        {
            string publicPrefix = _publicBaseUrl + "/";

            if (url.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = publicPrefix;
            }
        }

        if (prefix is null)
        {
            return null;
        }

        return Uri.UnescapeDataString(
            url[prefix.Length..].Split('?')[0]);
    }

    public void Dispose() => _client.Dispose();
}
