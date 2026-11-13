using Microsoft.Extensions.Options;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Ports;
using Qwek.Applications.UseCases;

namespace Qwek.Interfaces.Endpoints;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(
        this IEndpointRouteBuilder app, string basePath)
    {
        string prefix = "/" + basePath.Trim('/');

        RouteGroupBuilder files = app.MapGroup(prefix)
            .WithTags("Files")
            .RequireAuthorization();

        files.MapGet("/avatars/{fileName}", GetAvatarAsync);
        files.MapGet("/attachments/{fileName}", GetAttachmentAsync);

        return app;
    }

    private static async Task<IResult> GetAvatarAsync(
        string fileName,
        IFileStorage storage,
        IOptions<StorageOptions> options,
        CancellationToken ct)
    {
        if (!IsValidFileName(fileName))
        {
            return Results.NotFound();
        }

        string url = $"{GetBasePath(options)}/avatars/{fileName}";
        StoredFile? file = await storage.OpenReadAsync(url, ct);

        return file is null
            ? Results.NotFound()
            : Results.File(
                file.Stream, file.ContentType, enableRangeProcessing: true);
    }

    private static async Task<IResult> GetAttachmentAsync(
        string fileName,
        IFileStorage storage,
        IOptions<StorageOptions> options,
        Messages messages,
        CancellationToken ct)
    {
        if (!IsValidFileName(fileName))
        {
            return Results.NotFound();
        }

        string url = $"{GetBasePath(options)}/attachments/{fileName}";
        await messages.EnsureAttachmentAccessAsync(url, ct);

        StoredFile? file = await storage.OpenReadAsync(url, ct);

        return file is null
            ? Results.NotFound()
            : Results.File(
                file.Stream, file.ContentType, enableRangeProcessing: true);
    }

    private static string GetBasePath(IOptions<StorageOptions> options) =>
        "/" + options.Value.PrivateBasePath.Trim('/');

    private static bool IsValidFileName(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        fileName is not "." and not ".." &&
        fileName == Path.GetFileName(fileName) &&
        !fileName.Contains('\\') &&
        fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
}
