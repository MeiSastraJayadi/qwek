using Qwek.Applications.Models;

namespace Qwek.Applications.Ports;

public interface IIdentityService
{
    Task<RegisterResult> RegisterAsync(
        string username, string displayName, 
        string email, string password, CancellationToken ct); 
    Task<AuthenticatedUser?> ValidateCredentialAsync(
        string usernameOrEmail, string password, CancellationToken ct);

    Task<UserProfile?> GetProfileAsync(string userId, CancellationToken ct);

    Task<ProfileUpdateResult> UpdateDisplayNameAsync(
        string userId, string displayName, CancellationToken ct);

    Task<ProfileUpdateResult> ChangeUsernameAsync(
        string userId, string username, CancellationToken ct);

    Task<ProfileUpdateResult> UpdateAvatarAsync(
        string userId, string? avatarUrl, long avatarSize, CancellationToken ct);

    Task<int> GetTokenVersionAsync(string userId, CancellationToken ct);

    Task<bool> IncrementTokenVersionAsync(string userId, CancellationToken ct);

    Task<long> GetAvatarSizeAsync(string userId, CancellationToken ct);
}
