namespace RealTimeApps.Domains.Entities;

public class Channel
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Topic { get; private set; }
    public Guid ServerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public ICollection<Message> Messages { get; private set; } = [];
    private Channel() {}

    public Channel(string name, string? topic, Guid serverId)
    {
        SetName(name);
        Topic = topic;
        ServerId = serverId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name) => SetName(name);
    public void ChangeTopic(string topic) => Topic = topic;

    private void SetName(string name)
    {
        if(string.IsNullOrWhiteSpace(name)) 
            throw new ArgumentException("Channel name is required", nameof(name));
        
        Name = name.Trim();
    }

}
