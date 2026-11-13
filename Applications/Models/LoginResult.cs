namespace Qwek.Applications.Models;

public record class LoginResult(string AccessToken, AuthenticatedUser User);
