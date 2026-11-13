using Qwek.Applications.Models;
using Qwek.Applications.Ports;
using Qwek.Domains.Entities;
using Qwek.Domains.Interfaces;

namespace Qwek.Applications.UseCases;

public class Servers
{
    private readonly IServerRepository _serverRepository; 
    private readonly IServerMemberRepository _serverMemberRepository;
    private readonly IUnitOfWork _unitOfWork; 
    private readonly ICurrentUser _currentUser;
    private readonly IRealtimeNotifier _realtimeNotifier;
    private readonly IUserDirectory _userDirectory;
    private readonly IInviteCodeGenerator _inviteCodeGenerator;

    public Servers(
        IServerRepository serverRepository, 
        IServerMemberRepository serverMemberRepository, 
        IUnitOfWork unitOfWork, 
        ICurrentUser currentUser, 
        IRealtimeNotifier realtimeNotifier,
        IUserDirectory userDirectory,
        IInviteCodeGenerator inviteCodeGenerator
    )
    {
        _serverRepository = serverRepository; 
        _serverMemberRepository = serverMemberRepository; 
        _unitOfWork = unitOfWork; 
        _currentUser = currentUser; 
        _realtimeNotifier = realtimeNotifier;
        _userDirectory = userDirectory;
        _inviteCodeGenerator = inviteCodeGenerator;
    }

    private string GetUserId()
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedAccessException("Authentication is required");
        
        return _currentUser.UserId;
    }


    public async Task<ServerDto> CreateAsync(
        string name, string? avatarUrl, CancellationToken ct
    )
    {
        string userId = GetUserId();
        Server server = new Server(
            name, userId, avatarUrl, _inviteCodeGenerator.Generate()); 
        await _serverRepository.AddAsync(server, ct);
        await _serverMemberRepository.AddAsync(new ServerMember(userId, server.Id), ct);
        await _unitOfWork.SaveChangesAsync(ct); 

        return ToDto(server);
    }

    private static ServerDto ToDto(Server server) => 
        new(server.Id, server.Name, server.AvatarUrl, server.OwnerId, server.CreatedAt);


    public async Task<IReadOnlyList<ServerDto>> GetMineAsync(CancellationToken ct)
    {
        string userId = GetUserId(); 
        IReadOnlyList<Server> servers = await _serverRepository.GetForUserAsync(userId, ct);
        return servers.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<ServerMemberDto>> GetMemberAsync(
        Guid serverId, CancellationToken ct
    )
    {
        string userId = GetUserId();

        if (!await _serverMemberRepository.IsMemberAsync(userId, serverId, ct))
            throw new UnauthorizedAccessException("You are not the member of this server");
        

        IReadOnlyList<ServerMember> members = await _serverMemberRepository
            .GetMemberAsync(serverId, ct);

        IReadOnlyDictionary<string, UserProfile> profiles =
            await _userDirectory.GetProfilesAsync(
                members.Select(member => member.PersonalUserId).Distinct().ToList(),
                ct);

        return members
            .Select(member =>
            {
                profiles.TryGetValue(member.PersonalUserId, out UserProfile? profile);

                return new ServerMemberDto(
                    member.PersonalUserId,
                    serverId,
                    profile?.UserName ?? string.Empty,
                    profile?.DisplayName ?? "Unknown",
                    member.CreatedAt);
            })
            .ToList();
    }

    public async Task JoinAsync(
        Guid serverId, string inviteCode, CancellationToken ct)
    {
        string userId = GetUserId(); 

        if (await _serverMemberRepository.IsMemberAsync(userId, serverId, ct))
            return;
        
        Server server = await _serverRepository.GetByIdAsync(serverId, ct)
            ?? throw new KeyNotFoundException("Server was not found");

        if (!string.Equals(
                server.InviteCode,
                inviteCode?.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Invalid invite code");
        }
        
        await _serverMemberRepository.AddAsync(new ServerMember(userId, serverId), ct);
        await _unitOfWork.SaveChangesAsync(ct); 

        await _realtimeNotifier.MemberJoinedServerAsync(serverId, userId, ct);
    }

    public async Task<ServerInviteDto> GetInviteAsync(
        Guid serverId, CancellationToken ct)
    {
        string userId = GetUserId();

        if (!await _serverMemberRepository.IsMemberAsync(userId, serverId, ct))
            throw new UnauthorizedAccessException("You are not the member of this server");

        Server server = await _serverRepository.GetByIdAsync(serverId, ct)
            ?? throw new KeyNotFoundException("Server was not found");

        return new ServerInviteDto(server.Id, server.InviteCode);
    }

    public async Task LeaveAsync(Guid serverId, CancellationToken ct)
    {
        string userId = GetUserId(); 
        Server? server = await _serverRepository.GetByIdAsync(serverId, ct)
            ?? throw new KeyNotFoundException("Server was not found");
        
        if (server.OwnerId == userId)
            throw new InvalidOperationException("The owner cannot leave their server"); 
        
        if (!await _serverMemberRepository.IsMemberAsync(userId, serverId, ct))
            return;
        
        await _serverMemberRepository.RemoveAsync(userId, serverId, ct); 
        await _unitOfWork.SaveChangesAsync(ct); 

        await _realtimeNotifier.MemberLeftServerAsync(serverId, userId, ct); 
    }

    public async Task DeleteAsync(Guid serverId, CancellationToken ct)
    {
        string userId = GetUserId(); 
        Server? server = await _serverRepository.GetByIdAsync(serverId, ct)
            ?? throw new KeyNotFoundException("Server was not found");
        
        if (server.OwnerId != userId)
            throw new UnauthorizedAccessException("Only the owner can delete this server"); 
        
        _serverRepository.Remove(server); 
        await _unitOfWork.SaveChangesAsync(ct);
    }

}
