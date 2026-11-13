namespace Qwek.Applications.Models;

public record class ProfileUpdateResult(
    bool Succeeded, IReadOnlyList<string> Errors, UserProfile? Profile
);
