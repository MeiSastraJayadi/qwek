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
        me.MapPost("/avatar/upload", UploadAvatarAsync)
            .DisableAntiforgery()
            .RequireRateLimiting("uploads");

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
        UpdateAvatarRequest request,
        Users users,
        IFileStorage storage,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            return Fail("Use the avatar upload endpoint to change your avatar.");
        }

        UserProfile? profile = await users.GetProfileAsync(ct);

        if (profile is null)
        {
            return Results.NotFound();
        }

        ProfileUpdateResult result = await users.ClearAvatarAsync(ct);

        if (result.Succeeded && profile.AvatarUrl is not null)
        {
            try
            {
                await storage.DeleteAsync(
                    profile.AvatarUrl, CancellationToken.None);
            }
            catch
            {
            }
        }

        return ToResult(result);
    }

    private static async Task<IResult> UploadAvatarAsync(
        IFormFile file,
        Users users,
        IFileStorage storage,
        IFileScanner scanner,
        IStorageQuota quota,
        ICurrentUser currentUser,
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

        string userId = currentUser.UserId ?? string.Empty;

        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        string contentType = extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };

        await using (Stream scanStream = file.OpenReadStream())
        {
            if (!await scanner.IsCleanAsync(scanStream, ct))
            {
                return Results.UnprocessableEntity(
                    new ProfileUpdateResult(
                        false,
                        ["Avatar failed the security scan."],
                        null));
            }
        }

        long previousAvatarSize = await users.GetAvatarSizeAsync(ct);
        StorageQuotaReservation reservation = await quota.ReserveAsync(
            userId, file.Length, previousAvatarSize, ct);

        string avatarUrl;

        try
        {
            await using Stream stream = file.OpenReadStream();
            avatarUrl = await storage.SaveAsync(
                "avatars", file.FileName, file.Length,
                contentType, stream, ct);
        }
        catch
        {
            await ReleaseQuietlyAsync(quota, reservation);
            throw;
        }

        ProfileUpdateResult result;

        try
        {
            result = await users.SetAvatarAsync(avatarUrl, file.Length, ct);
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

            await ReleaseQuietlyAsync(quota, reservation);
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

            await ReleaseQuietlyAsync(quota, reservation);
            return ToResult(result);
        }

        await CompleteQuietlyAsync(quota, reservation);

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

    private static async Task CompleteQuietlyAsync(
        IStorageQuota quota, StorageQuotaReservation reservation)
    {
        try
        {
            await quota.CompleteAsync(
                reservation, CancellationToken.None);
        }
        catch
        {
        }
    }

    private static async Task ReleaseQuietlyAsync(
        IStorageQuota quota, StorageQuotaReservation reservation)
    {
        try
        {
            await quota.ReleaseAsync(
                reservation, CancellationToken.None);
        }
        catch
        {
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
