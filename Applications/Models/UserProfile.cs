namespace RealTimeApps.Applications.Models;

public record class UserProfile(
    string UserId, string UserName, string DisplayName, string? Email,
    string? AvatarUrl = null
);
