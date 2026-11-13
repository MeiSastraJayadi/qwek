namespace Qwek.Applications.Models;

public record class AuthenticatedUser(
    string UserId, string UserName, string DisplayName);
