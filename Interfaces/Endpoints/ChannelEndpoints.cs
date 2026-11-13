using RealTimeApps.Applications.Models;
using RealTimeApps.Applications.UseCases;

namespace RealTimeApps.Interfaces.Endpoints;

public static class ChannelEndpoints
{
    public static IEndpointRouteBuilder MapChannelEndpoints(
        this IEndpointRouteBuilder app
    )
    {

        RouteGroupBuilder serverChannels = app
            .MapGroup("/api/servers/{serverId:guid}/channels")
            .WithTags("Channels")
            .RequireAuthorization();

        serverChannels.MapPost("/", CreateAsync); 
        serverChannels.MapGet("/", GetByServerAsync);
        
        RouteGroupBuilder channels = app.MapGroup("/channel")
            .WithTags("Channels")
            .RequireAuthorization();

        channels.MapPatch("/{channelId:int}", RenameAsync);
        channels.MapPatch("/{channelId:int}/topic", ChangeTopicAsync);
        channels.MapDelete("/{channelId:int}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        Guid serverId, CreateChannelRequest request, 
        Channels channels, CancellationToken ct
    )
    {
        ChannelDto channel = await channels.CreateAsync(
            serverId, request.Name, request.Topic, ct
        );

        return Results.Created($"/api/channels/{channel.Id}", channel); 
    }

    private static async Task<IResult> GetByServerAsync(
        Guid serverId, Channels channels, CancellationToken ct
    )
    {
        IReadOnlyList<ChannelDto> result = await channels.GetByServerAsync(serverId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ChangeTopicAsync(
        int channelId, ChangeTopicRequest request, Channels channels, 
        CancellationToken ct
    )
    {
        await channels.ChangeTopicAsync(channelId, request.Topic, ct);
        return Results.NoContent(); 
    }

    private static async Task<IResult> RenameAsync(
        int channelId, RenameChannelRequest request, 
        Channels channels, CancellationToken ct
    )
    {
        await channels.RenameAsync(channelId, request.Name, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(
        int channelId, Channels channels, CancellationToken ct
    )
    {
        await channels.DeleteAsync(channelId, ct);
        return Results.NoContent();
    }

    public sealed record CreateChannelRequest(
        string Name, string? Topic);
    public sealed record RenameChannelRequest(string Name);
    public sealed record ChangeTopicRequest(string Topic);

}
