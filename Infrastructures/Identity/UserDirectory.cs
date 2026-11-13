using Microsoft.EntityFrameworkCore;
using RealTimeApps.Applications.Models;
using RealTimeApps.Applications.Ports;
using RealTimeApps.Infrastructures.Persistence;

namespace RealTimeApps.Infrastructures.Identity;

public sealed class UserDirectory : IUserDirectory
{
    private readonly AppDbContext _db;

    public UserDirectory(AppDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<string, UserProfile>> GetProfilesAsync(
        IReadOnlyCollection<string> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<string, UserProfile>();
        }

        List<string> ids = userIds.Distinct().ToList();

        var users = await _db.Users
            .Where(user => ids.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                user.UserName,
                user.DisplayName,
                user.Email,
                user.AvatarUrl
            })
            .ToListAsync(ct);

        return users.ToDictionary(
            user => user.Id,
            user =>
            {
                string userName = user.UserName ?? string.Empty;
                string displayName = !string.IsNullOrWhiteSpace(user.DisplayName)
                    ? user.DisplayName
                    : !string.IsNullOrWhiteSpace(userName)
                        ? userName
                        : "Unknown";

                return new UserProfile(
                    user.Id, userName, displayName, user.Email, user.AvatarUrl);
            });
    }
}
