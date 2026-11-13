using Qwek.Applications.Models;

namespace Qwek.Applications.Ports;

public interface IUserDirectory
{
    Task<IReadOnlyDictionary<string, UserProfile>> GetProfilesAsync(
        IReadOnlyCollection<string> userIds, CancellationToken ct);
}
