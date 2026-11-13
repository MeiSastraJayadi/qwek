using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Models;
using Qwek.Applications.Ports;
using Qwek.Domains.Entities;

namespace Qwek.Applications.UseCases;

public class Auth
{
    private readonly IIdentityService _identityService; 
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly JwtOptions _jwtOptions;

    public Auth(
        IIdentityService identityService,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        IOptions<JwtOptions> jwtOptions)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _jwtOptions = jwtOptions.Value;
    }

    public Task<RegisterResult> RegisterAsync(
        string username, 
        string displayName, 
        string email, 
        string password, 
        CancellationToken ct
    )
    {
        return _identityService
            .RegisterAsync(username, displayName, email, password, ct);
    }

    public async Task<LoginResult?> LoginAsync(
        string usernameOrEmail, 
        string password,
        CancellationToken ct
    )
    {
        AuthenticatedUser? user = await _identityService
            .ValidateCredentialAsync(usernameOrEmail, password, ct);

        if (user is null) return null; 

        int tokenVersion = await _identityService
            .GetTokenVersionAsync(user.UserId, ct);
        string accessToken = _tokenService
            .CreateAccessToken(user.UserId, user.UserName, tokenVersion);

        (string refreshToken, RefreshToken stored) =
            CreateRefreshToken(user.UserId);
        await _refreshTokens.AddAsync(stored, ct);

        return new LoginResult(
            accessToken, user, refreshToken, stored.ExpiresAt);
    }

    public async Task<LoginResult?> RefreshAsync(
        string refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        RefreshToken? stored = await _refreshTokens.GetByHashAsync(
            Hash(refreshToken.Trim()), ct);

        if (stored is null || !stored.IsActive)
        {
            return null;
        }

        UserProfile? profile = await _identityService
            .GetProfileAsync(stored.UserId, ct);

        if (profile is null ||
            !await _refreshTokens.TryRevokeAsync(stored, ct))
        {
            return null;
        }

        int tokenVersion = await _identityService
            .GetTokenVersionAsync(stored.UserId, ct);
        string accessToken = _tokenService.CreateAccessToken(
            stored.UserId, profile.UserName, tokenVersion);

        (string newRefreshToken, RefreshToken newStored) =
            CreateRefreshToken(stored.UserId);
        await _refreshTokens.AddAsync(newStored, ct);

        return new LoginResult(
            accessToken,
            new AuthenticatedUser(
                stored.UserId, profile.UserName, profile.DisplayName),
            newRefreshToken,
            newStored.ExpiresAt);
    }

    public async Task LogoutAsync(string userId, CancellationToken ct)
    {
        await _identityService.IncrementTokenVersionAsync(userId, ct);
        await _refreshTokens.RevokeAllForUserAsync(userId, ct);
    }

    private (string Token, RefreshToken Stored) CreateRefreshToken(
        string userId)
    {
        string token = _tokenService.CreateRefreshToken();
        DateTime expiresAt = DateTime.UtcNow
            .AddDays(_jwtOptions.RefreshExpiryDays);

        return (token, new RefreshToken(userId, Hash(token), expiresAt));
    }

    private static string Hash(string token) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
