using Microsoft.AspNetCore.Identity;
using RealTimeApps.Applications.Models;
using RealTimeApps.Applications.Ports;
using RealTimeApps.Models;

namespace RealTimeApps.Infrastructures.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<PersonalUser> _userManager; 

    public IdentityService(UserManager<PersonalUser> userManager) => 
        _userManager = userManager;

    public async Task<RegisterResult> RegisterAsync(
        string username, string displayName, string email, 
        string password, CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        PersonalUser user = new()
        {
            UserName = username, 
            Email = email, 
            DisplayName = string.IsNullOrEmpty(displayName) ? username : displayName
        };

        IdentityResult result = await _userManager.CreateAsync(user, password);

        if(!result.Succeeded)
        {
            IReadOnlyList<string> errors = result.Errors 
                .Select(e => e.Description)
                .ToList();

            return new RegisterResult(false, null, errors);
        }

        return new RegisterResult(true, user.Id, []);
    }

    public async Task<AuthenticatedUser?> ValidateCredentialAsync(
        string usernameOrEmail, string password, CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        PersonalUser? user = await _userManager.FindByNameAsync(usernameOrEmail)
            ?? await _userManager.FindByEmailAsync(usernameOrEmail); 

        if (user is null)
            return null;

        bool passwordValid = await _userManager.CheckPasswordAsync(user, password); 

        if (!passwordValid) return null; 

        return new AuthenticatedUser(
            user.Id, 
            user.UserName ?? string.Empty, 
            user.DisplayName
        );
    }

    public async Task<UserProfile?> GetProfileAsync(
        string userId, CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        PersonalUser? user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : ToProfile(user);
    }

    public async Task<ProfileUpdateResult> UpdateDisplayNameAsync(
        string userId, string displayName, CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(displayName))
            return Fail("Display name is required");

        PersonalUser? user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return Fail("User was not found");

        user.DisplayName = displayName.Trim();
        return await SaveAsync(user);
    }

    public async Task<ProfileUpdateResult> ChangeUsernameAsync(
        string userId, string username, CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(username))
            return Fail("Username is required");

        PersonalUser? user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return Fail("User was not found");

        IdentityResult result = await _userManager.SetUserNameAsync(user, username.Trim());
        return ToResult(result, user);
    }

    public async Task<ProfileUpdateResult> UpdateAvatarAsync(
        string userId, string? avatarUrl, CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        PersonalUser? user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return Fail("User was not found");

        user.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        return await SaveAsync(user);
    }

    private async Task<ProfileUpdateResult> SaveAsync(PersonalUser user)
    {
        IdentityResult result = await _userManager.UpdateAsync(user);
        return ToResult(result, user);
    }

    private static ProfileUpdateResult ToResult(
        IdentityResult result, PersonalUser user
    ) =>
        result.Succeeded
            ? new ProfileUpdateResult(true, [], ToProfile(user))
            : new ProfileUpdateResult(
                false,
                result.Errors.Select(e => e.Description).ToList(),
                null);

    private static ProfileUpdateResult Fail(string error) =>
        new(false, [error], null);

    private static UserProfile ToProfile(PersonalUser user) =>
        new(user.Id, user.UserName ?? string.Empty, user.DisplayName,
            user.Email, user.AvatarUrl);
}
