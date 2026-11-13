using RealTimeApps.Applications.Models;
using RealTimeApps.Applications.UseCases;

namespace RealTimeApps.Interfaces.Endpoints;

public static class ServerEndpoints
{

    public static IEndpointRouteBuilder MapServerEndpoints(
        this IEndpointRouteBuilder app
    )
    {
        RouteGroupBuilder servers = app.MapGroup("/api/servers")
            .WithTags("Servers")
            .RequireAuthorization();

        servers.MapPost("/", CreateAsync); 
        servers.MapGet("/", GetMineAsync); 
        servers.MapGet("/{serverId:guid}/members", GetMemberAsync); 
        servers.MapPost("/{serverId:guid}/join", JoinAsync); 
        servers.MapPost("/{serverId:guid}/leave", LeaveAsync); 
        servers.MapDelete("/{serverId:guid}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateServerRequest request, Servers servers, CancellationToken ct
    )
    {
        ServerDto server = await servers.CreateAsync(
            request.Name, request.AvatarUrl, ct
        );

        return Results.Created($"/api/servers/{server.Id}", server);
    }

    private static async Task<IResult> GetMineAsync(
        Servers servers, CancellationToken ct
    )
    {
        IReadOnlyList<ServerDto> result = await servers.GetMineAsync(ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetMemberAsync(
        Guid serverId, Servers servers, CancellationToken ct
    )
    {
        IReadOnlyList<ServerMemberDto> result = 
            await servers.GetMemberAsync(serverId, ct);
        
        return Results.Ok(result);
    }

    private static async Task<IResult> JoinAsync(
        Guid serverId, Servers servers, CancellationToken ct
    )
    {
        await servers.JoinAsync(serverId, ct); 
        return Results.NoContent();
    }

    private static async Task<IResult> LeaveAsync(
        Guid serverId, Servers servers, CancellationToken ct
    )
    {
        await servers.LeaveAsync(serverId, ct); 
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(
        Guid serverId, Servers servers, CancellationToken ct
    )
    {
        await servers.DeleteAsync(serverId, ct); 
        return Results.NoContent();
    }

    public sealed record CreateServerRequest(string Name, string? AvatarUrl);

}
