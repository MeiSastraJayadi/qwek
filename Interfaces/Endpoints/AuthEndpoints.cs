using Qwek.Applications.Models;
using Qwek.Applications.Ports;
using Qwek.Applications.UseCases;

namespace Qwek.Interfaces.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {

        RouteGroupBuilder group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth");

        group.MapPost("/register", RegisterAsync).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/refresh", RefreshAsync).AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request, Auth auth, CancellationToken ct
    )
    {
        RegisterResult result = await auth.RegisterAsync(
            request.Username, 
            request.Displayname, 
            request.Email, 
            request.Password, 
            ct
        );

        return result.Succeeded
            ? Results.Created($"/auth/users/{result.UserId}", result)
            : Results.BadRequest(result);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, Auth auth, CancellationToken ct
    )
    {
        LoginResult? result = await auth.LoginAsync(
            request.UsernameOrEmail, request.Password, ct
        );

        return result is null 
            ? Results.Unauthorized()
            : Results.Ok(result);
    }

    private static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request, Auth auth, CancellationToken ct
    )
    {
        LoginResult? result = await auth.RefreshAsync(
            request.RefreshToken, ct);

        return result is null
            ? Results.Unauthorized()
            : Results.Ok(result);
    }

    private static async Task<IResult> LogoutAsync(
        ICurrentUser currentUser, Auth auth, CancellationToken ct
    )
    {
        if (!currentUser.IsAuthenticated ||
            string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return Results.Unauthorized();
        }

        await auth.LogoutAsync(currentUser.UserId, ct);
        return Results.NoContent();
    }

    public sealed record RegisterRequest(
        string Username, string Displayname, string Email, 
        string Password
    );

    public sealed record LoginRequest(
        string UsernameOrEmail, string Password
    );

    public sealed record RefreshTokenRequest(string RefreshToken);
}
