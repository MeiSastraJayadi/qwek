using RealTimeApps.Applications.Models;

namespace RealTimeApps.Applications.Ports;

public interface IUserDirectory
{
    Task<IReadOnlyDictionary<string, UserProfile>> GetProfilesAsync(
        IReadOnlyCollection<string> userIds, CancellationToken ct);
}
