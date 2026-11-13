namespace Qwek.Interfaces.RealTime;

public sealed class PresenceTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<string, string> _connections = [];
    private readonly Dictionary<string, int> _userConnections = [];

    public bool AddConnection(string connectionId, string userId)
    {
        lock (_gate)
        {
            _connections[connectionId] = userId;
            _userConnections.TryGetValue(userId, out int count);
            _userConnections[userId] = count + 1;
            return count == 0;
        }
    }

    public (bool WentOffline, string? UserId) RemoveConnection(string connectionId)
    {
        lock (_gate)
        {
            if (!_connections.Remove(connectionId, out string? userId))
            {
                return (false, null);
            }

            _userConnections.TryGetValue(userId, out int count);

            if (count <= 1)
            {
                _userConnections.Remove(userId);
                return (true, userId);
            }

            _userConnections[userId] = count - 1;
            return (false, userId);
        }
    }

    public IReadOnlyList<string> GetOnlineUserIds()
    {
        lock (_gate)
        {
            return _userConnections.Keys.ToList();
        }
    }
}
