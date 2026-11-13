namespace RealTimeApps.Applications.Models;

public record class ChannelDto(
    int Id, string Name, string? Topic, Guid ServerId, DateTime CreatedAt
);
