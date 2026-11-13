namespace Qwek.Applications.Models;

public record class ServerDto(
    Guid Id, string Name, string? AvatarUrl, string OwnerId, DateTime CreatedAt
);
