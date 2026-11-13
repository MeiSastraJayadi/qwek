namespace Qwek.Domains.Entities;

public class Server
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty; 
    public string? AvatarUrl { get; private set; }
    public string OwnerId { get; private set; } = string.Empty; 
    public string InviteCode { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public ICollection<Channel> Channels { get; private set; } = [];
    private Server() {}

    public Server(string name, string ownerId, string? avatarUrl, string inviteCode)
    {
        Id = Guid.NewGuid();
        SetName(name); 
        OwnerId = ownerId;
        AvatarUrl = avatarUrl; 
        SetInviteCode(inviteCode);
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name) => SetName(name);

    public void SetAvatar(string? avatarUrl) => AvatarUrl = avatarUrl;

    private void SetInviteCode(string inviteCode)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            throw new ArgumentException("Invite code is required", nameof(inviteCode));
        InviteCode = inviteCode.Trim();
    }

    private void SetName (string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Server name is required", nameof(name));
        Name = name.Trim();
    }
}
