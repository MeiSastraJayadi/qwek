using Qwek.Applications.Models;
using Qwek.Applications.UseCases;

namespace Qwek.Interfaces.Endpoints;

public static class UserEndpoints
{
    private const long MaxAvatarBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AvatarExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif"
        };

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder me = app.MapGroup("/api/users/me")
            .WithTags("Users")
            .RequireAuthorization();

        me.MapGet("/", GetProfileAsync);
        me.MapPatch("/display-name", UpdateDisplayNameAsync);
        me.MapPatch("/username", UpdateUsernameAsync);
        me.MapPatch("/avatar", UpdateAvatarAsync);
        me.MapPost("/avatar/upload", UploadAvatarAsync).DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> GetProfileAsync(
        Users users, CancellationToken ct)
    {
        UserProfile? profile = await users.GetProfileAsync(ct);
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static async Task<IResult> UpdateDisplayNameAsync(
        UpdateDisplayNameRequest request, Users users, CancellationToken ct)
    {
        ProfileUpdateResult result = await users
            .UpdateDisplayNameAsync(request.DisplayName, ct);

        return ToResult(result);
    }

    private static async Task<IResult> UpdateUsernameAsync(
        UpdateUsernameRequest request, Users users, CancellationToken ct)
    {
        ProfileUpdateResult result = await users
            .ChangeUsernameAsync(request.Username, ct);

        return ToResult(result);
    }

    private static async Task<IResult> UpdateAvatarAsync(
        UpdateAvatarRequest request, Users users, CancellationToken ct)
    {
        ProfileUpdateResult result = await users
            .UpdateAvatarAsync(request.AvatarUrl, ct);

        return ToResult(result);
    }

    private static async Task<IResult> UploadAvatarAsync(
        IFormFile file,
        Users users,
        IWebHostEnvironment environment,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Fail("Choose an image file to upload.");
        }

        if (file.Length > MaxAvatarBytes)
        {
            return Fail("Avatar must be 5 MB or smaller.");
        }

        string extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AvatarExtensions.Contains(extension))
        {
            return Fail("Avatar must be a PNG, JPG, or GIF image.");
        }

        UserProfile? profile = await users.GetProfileAsync(ct);

        if (profile is null)
        {
            return Results.NotFound();
        }

        string avatarDirectory = Path.Combine(
            environment.ContentRootPath, "uploads", "avatars");
        Directory.CreateDirectory(avatarDirectory);

        string fileName =
            $"{profile.UserId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}" +
            extension.ToLowerInvariant();
        string filePath = Path.Combine(avatarDirectory, fileName);

        await using (FileStream stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, ct);
        }

        DeletePreviousAvatar(profile.AvatarUrl, avatarDirectory);

        ProfileUpdateResult result = await users.UpdateAvatarAsync(
            $"/uploads/avatars/{fileName}", ct);

        return ToResult(result);
    }

    private static void DeletePreviousAvatar(
        string? avatarUrl,
        string avatarDirectory)
    {
        const string prefix = "/uploads/avatars/";

        if (string.IsNullOrWhiteSpace(avatarUrl) ||
            !avatarUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string? previous = avatarUrl[prefix.Length..].Split('?')[0];
        string fileName = Path.GetFileName(previous);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        string path = Path.Combine(avatarDirectory, fileName);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static IResult Fail(string error) =>
        Results.BadRequest(new ProfileUpdateResult(false, [error], null));

    private static IResult ToResult(ProfileUpdateResult result) =>
        result.Succeeded
            ? Results.Ok(result.Profile)
            : Results.BadRequest(result);

    public sealed record UpdateDisplayNameRequest(string DisplayName);
    public sealed record UpdateUsernameRequest(string Username);
    public sealed record UpdateAvatarRequest(string? AvatarUrl);
}
