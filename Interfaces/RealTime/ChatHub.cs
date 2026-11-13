using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RealTimeApps.Applications.Models;
using RealTimeApps.Applications.Ports;
using RealTimeApps.Domains.Entities;
using RealTimeApps.Domains.Interfaces;

namespace RealTimeApps.Interfaces.RealTime;

[Authorize]
public class ChatHub : Hub
{
    private readonly PresenceTracker _presence;
    private readonly IChannelRepository _channels;
    private readonly IServerMemberRepository _members;
    private readonly IUserDirectory _users;

    public ChatHub(
        PresenceTracker presence,
        IChannelRepository channels,
        IServerMemberRepository members,
        IUserDirectory users)
    {
        _presence = presence;
        _channels = channels;
        _members = members;
        _users = users;
    }

    public Task JoinChannel(int channelId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"channel:{channelId}");

    public Task LeavelChannel(int channelId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");

    public Task LeaveChannel(int channelId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");

    public async Task Typing(int channelId, bool isTyping)
    {
        string? userId = Context.UserIdentifier;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        CancellationToken ct = Context.ConnectionAborted;
        Channel? channel = await _channels.GetByIdAsync(channelId, ct);

        if (channel is null ||
            !await _members.IsMemberAsync(userId, channel.ServerId, ct))
        {
            return;
        }

        IReadOnlyDictionary<string, UserProfile> profiles =
            await _users.GetProfilesAsync([userId], ct);
        profiles.TryGetValue(userId, out UserProfile? profile);

        string userName = Context.User?.FindFirstValue(ClaimTypes.Name)
            ?? profile?.UserName
            ?? string.Empty;
        string displayName = profile?.DisplayName
            ?? (string.IsNullOrWhiteSpace(userName) ? "Unknown" : userName);

        await Clients
            .OthersInGroup($"channel:{channelId}")
            .SendAsync("UserTyping", new
            {
                channelId,
                userId,
                userName,
                displayName,
                isTyping
            }, ct);
    }

    public override async Task OnConnectedAsync()
    {
        string? userId = Context.UserIdentifier;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            bool becameOnline = _presence.AddConnection(
                Context.ConnectionId,
                userId);

            if (becameOnline)
            {
                await Clients.All.SendAsync(
                    "PresenceChanged",
                    new { userId, isOnline = true },
                    Context.ConnectionAborted);
            }

            await Clients.Caller.SendAsync(
                "PresenceSnapshot",
                _presence.GetOnlineUserIds(),
                Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        (bool wentOffline, string? userId) =
            _presence.RemoveConnection(Context.ConnectionId);

        if (wentOffline && !string.IsNullOrWhiteSpace(userId))
        {
            await Clients.All.SendAsync(
                "PresenceChanged",
                new { userId, isOnline = false });
        }

        await base.OnDisconnectedAsync(exception);
    }
}
