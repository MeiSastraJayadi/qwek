using Qwek.Applications.Models;
using Qwek.Applications.Ports;

namespace Qwek.Applications.UseCases;

public class Users
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUser _currentUser;

    public Users(IIdentityService identityService, ICurrentUser currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public Task<UserProfile?> GetProfileAsync(CancellationToken ct) =>
        _identityService.GetProfileAsync(GetUserId(), ct);

    public Task<ProfileUpdateResult> UpdateDisplayNameAsync(
        string displayName, CancellationToken ct) =>
        _identityService.UpdateDisplayNameAsync(GetUserId(), displayName, ct);

    public Task<ProfileUpdateResult> ChangeUsernameAsync(
        string username, CancellationToken ct) =>
        _identityService.ChangeUsernameAsync(GetUserId(), username, ct);

    public Task<ProfileUpdateResult> UpdateAvatarAsync(
        string? avatarUrl, CancellationToken ct) =>
        _identityService.UpdateAvatarAsync(GetUserId(), avatarUrl, ct);

    private string GetUserId()
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedAccessException("Authentication is required");

        return _currentUser.UserId;
    }
}
