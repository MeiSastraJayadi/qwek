using Microsoft.Extensions.Options;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Models;
using Qwek.Applications.Ports;
using Qwek.Applications.UseCases;
using Qwek.Interfaces.Web;

namespace Qwek.Interfaces.Endpoints;

public static class UserEndpoints
{
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
        IFileStorage storage,
        IOptions<StorageOptions> options,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Fail("Choose an image file to upload.");
        }

        long maxAvatarBytes = options.Value.MaxAvatarBytes;

        if (file.Length > maxAvatarBytes)
        {
            long maxMegabytes = maxAvatarBytes / (1024 * 1024);
            return Fail($"Avatar must be {maxMegabytes} MB or smaller.");
        }

        string extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AvatarExtensions.Contains(extension))
        {
            return Fail("Avatar must be a PNG, JPG, or GIF image.");
        }

        if (!await FileSignatureValidator.MatchesExtensionAsync(
                file, extension, ct))
        {
            return Fail("Avatar content does not match its file extension.");
        }

        UserProfile? profile = await users.GetProfileAsync(ct);

        if (profile is null)
        {
            return Results.NotFound();
        }

        string contentType = extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };

        string avatarUrl;

        await using (Stream stream = file.OpenReadStream())
        {
            avatarUrl = await storage.SaveAsync(
                "avatars", file.FileName, file.Length,
                contentType, stream, ct);
        }

        ProfileUpdateResult result;

        try
        {
            result = await users.UpdateAvatarAsync(avatarUrl, ct);
        }
        catch
        {
            try
            {
                await storage.DeleteAsync(
                    avatarUrl, CancellationToken.None);
            }
            catch
            {
            }

            throw;
        }

        if (!result.Succeeded)
        {
            try
            {
                await storage.DeleteAsync(
                    avatarUrl, CancellationToken.None);
            }
            catch
            {
            }

            return ToResult(result);
        }

        try
        {
            await storage.DeleteAsync(
                profile.AvatarUrl, CancellationToken.None);
        }
        catch
        {
        }

        return ToResult(result);
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
