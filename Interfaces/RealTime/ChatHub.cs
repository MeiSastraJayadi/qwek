using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Qwek.Applications.Models;
using Qwek.Applications.Ports;
using Qwek.Domains.Entities;
using Qwek.Domains.Interfaces;

namespace Qwek.Interfaces.RealTime;

[Authorize]
public class ChatHub : Hub
{
    private readonly PresenceTracker _presence;
    private readonly IChannelRepository _channels;
    private readonly IServerMemberRepository _members;
    private readonly IUserDirectory _users;
    private readonly HubRateLimiter _rateLimiter;

    public ChatHub(
        PresenceTracker presence,
        IChannelRepository channels,
        IServerMemberRepository members,
        IUserDirectory users,
        HubRateLimiter rateLimiter)
    {
        _presence = presence;
        _channels = channels;
        _members = members;
        _users = users;
        _rateLimiter = rateLimiter;
    }

    public async Task JoinChannel(int channelId)
    {
        string? userId = Context.UserIdentifier;

        if (string.IsNullOrWhiteSpace(userId) ||
            !_rateLimiter.TryAcquire(
                userId, "join", 30, TimeSpan.FromMinutes(1)))
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

        await Groups.AddToGroupAsync(
            Context.ConnectionId, $"channel:{channelId}", ct);
    }

    public Task LeavelChannel(int channelId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");

    public Task LeaveChannel(int channelId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel:{channelId}");

    public async Task Typing(int channelId, bool isTyping)
    {
        string? userId = Context.UserIdentifier;

        if (string.IsNullOrWhiteSpace(userId) ||
            !_rateLimiter.TryAcquire(
                userId, "typing", 20, TimeSpan.FromSeconds(10)))
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
