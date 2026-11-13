using Microsoft.AspNetCore.Identity;

namespace Qwek.Models;

public class PersonalUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public long AvatarSize { get; set; }
    public int TokenVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
