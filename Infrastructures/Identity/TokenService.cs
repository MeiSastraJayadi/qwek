using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Qwek.Applications.Commons;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Ports;

namespace Qwek.Infrastructures.Identity;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options; 
    
    public TokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public string CreateAccessToken(
        string userId, string username, int tokenVersion)
    {
        Claim[] claims = [
            new(JwtRegisteredClaimNames.Sub, userId), 
            new(ClaimTypes.NameIdentifier, userId), 
            new(ClaimTypes.Name, username), 
            new(AuthClaims.TokenVersion, tokenVersion.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_options.Key)); 
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _options.Issuer, 
            audience: _options.Audience, 
            claims: claims, 
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
