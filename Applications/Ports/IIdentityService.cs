using RealTimeApps.Applications.Models;

namespace RealTimeApps.Applications.Ports;

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
        string userId, string? avatarUrl, CancellationToken ct);
}
