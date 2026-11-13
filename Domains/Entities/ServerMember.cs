namespace RealTimeApps.Domains.Entities;

public class ServerMember
{
    public string PersonalUserId { get; private set; } = string.Empty;
    public Guid ServerId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ServerMember() {}

    public ServerMember (string personalUserId, Guid serverId)
    {
        PersonalUserId = personalUserId;
        ServerId = serverId;
        CreatedAt = DateTime.UtcNow;
    }
    
}
