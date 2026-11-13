using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Qwek.Interfaces.Web;

public static class RateLimitPolicy
{
    public const string Auth = "auth";
    public const string Messages = "messages";
    public const string Uploads = "uploads";

    public static RateLimitPartition<string> Create(
        HttpContext context, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });

    public static string GetPartitionKey(HttpContext context)
    {
        string? userId = context.User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        return !string.IsNullOrWhiteSpace(userId)
            ? "user:" + userId
            : "ip:" + (context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown");
    }
}
