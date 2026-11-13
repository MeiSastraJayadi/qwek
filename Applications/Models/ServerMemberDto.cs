namespace Qwek.Applications.Models;

public record class ServerMemberDto(
    string UserId, Guid ServerId, string UserName, string DisplayName,
    string? Email, DateTime JoinedAt
);
