using Qwek.Applications.Models;
using Qwek.Applications.Ports;

namespace Qwek.Applications.UseCases;

public class Users
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _fileStorage;

    public Users(
        IIdentityService identityService,
        ICurrentUser currentUser,
        IFileStorage fileStorage)
    {
        _identityService = identityService;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public Task<UserProfile?> GetProfileAsync(CancellationToken ct) =>
        _identityService.GetProfileAsync(GetUserId(), ct);

    public Task<ProfileUpdateResult> UpdateDisplayNameAsync(
        string displayName, CancellationToken ct) =>
        _identityService.UpdateDisplayNameAsync(GetUserId(), displayName, ct);

    public Task<ProfileUpdateResult> ChangeUsernameAsync(
        string username, CancellationToken ct) =>
        _identityService.ChangeUsernameAsync(GetUserId(), username, ct);

    public Task<ProfileUpdateResult> ClearAvatarAsync(CancellationToken ct) =>
        _identityService.UpdateAvatarAsync(GetUserId(), null, 0, ct);

    public Task<ProfileUpdateResult> SetAvatarAsync(
        string avatarUrl, long avatarSize, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl) ||
            !_fileStorage.IsManagedUrl(avatarUrl.Trim(), "avatars"))
        {
            throw new ArgumentException(
                "Avatar URL must reference an uploaded avatar",
                nameof(avatarUrl));
        }

        return _identityService.UpdateAvatarAsync(
            GetUserId(), avatarUrl.Trim(), avatarSize, ct);
    }

    public Task<long> GetAvatarSizeAsync(CancellationToken ct) =>
        _identityService.GetAvatarSizeAsync(GetUserId(), ct);

    private string GetUserId()
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedAccessException("Authentication is required");

        return _currentUser.UserId;
    }
}
