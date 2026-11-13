namespace Qwek.Applications.Models;

public record class RegisterResult(
    bool Succeeded, string? UserId, IReadOnlyList<string> Errors);
