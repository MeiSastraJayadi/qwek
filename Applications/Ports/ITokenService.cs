namespace Qwek.Applications.Ports;

public interface ITokenService
{
    string CreateAccessToken(
        string userId, string username, int tokenVersion);
    string CreateRefreshToken();
}
