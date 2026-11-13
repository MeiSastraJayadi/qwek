using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Models;
using Qwek.Applications.Ports;
using Qwek.Applications.UseCases;
using Qwek.Interfaces.Web;

namespace Qwek.Interfaces.Endpoints;

public static class MessageEndpoints
{
    private static readonly Dictionary<string, string> AttachmentContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".pdf"] = "application/pdf",
            [".txt"] = "text/plain",
            [".md"] = "text/markdown",
            [".csv"] = "text/csv",
            [".mp4"] = "video/mp4",
            [".webm"] = "video/webm",
            [".mov"] = "video/quicktime",
            [".mp3"] = "audio/mpeg",
            [".wav"] = "audio/wav",
            [".ogg"] = "audio/ogg",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".zip"] = "application/zip"
        };

    public static IEndpointRouteBuilder MapMessageEndpoints(
        this IEndpointRouteBuilder app
    )
    {

        RouteGroupBuilder channelMessages = app
            .MapGroup("/api/channels/{channelId:int}/messages")
            .WithTags("Messages")
            .RequireAuthorization();

        channelMessages.MapPost("/", SendAsync); 
        channelMessages.MapPost("/upload", SendAttachmentAsync)
            .DisableAntiforgery();
        channelMessages.MapGet("/", GetHistoryAsync);

        RouteGroupBuilder messages = app.MapGroup("/api/messages")
            .WithTags("Messages")
            .RequireAuthorization();

        messages.MapPatch("/{messageId:int}", EditAsync);
        messages.MapDelete("/{messageId:int}", DeleteAsync);
        messages.MapPost("/{messageId:int}/reactions", ToggleReactionAsync);

        return app;
    } 


    private static async Task<IResult> SendAsync(
        int channelId, SendMessageRequest request, 
        Messages messages, CancellationToken ct
    )
    {
        MessageDto message = await messages.SendAsync(
            channelId,
            request.Content,
            ct,
            replyToMessageId: request.ReplyToMessageId);

        return Results.Created($"/api/messages/{message.Id}", message);
    }

    private static async Task<IResult> SendAttachmentAsync(
        int channelId,
        IFormFile file,
        [FromForm] string? content,
        Messages messages,
        IFileStorage storage,
        IOptions<StorageOptions> options,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new
            {
                errors = new[] { "Choose a file to send." }
            });
        }

        long maxAttachmentBytes = options.Value.MaxAttachmentBytes;

        if (file.Length > maxAttachmentBytes)
        {
            long maxMegabytes = maxAttachmentBytes / (1024 * 1024);
            return Results.BadRequest(new
            {
                errors = new[]
                {
                    $"Files must be {maxMegabytes} MB or smaller."
                }
            });
        }

        string extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AttachmentContentTypes.TryGetValue(
                extension, out string? contentType))
        {
            return Results.BadRequest(new
            {
                errors = new[] { "That file type is not supported." }
            });
        }

        if (!await FileSignatureValidator.MatchesExtensionAsync(
                file, extension, ct))
        {
            return Results.BadRequest(new
            {
                errors = new[] { "File content does not match its file extension." }
            });
        }

        string attachmentUrl;

        await using (Stream stream = file.OpenReadStream())
        {
            attachmentUrl = await storage.SaveAsync(
                "attachments", file.FileName, file.Length,
                contentType!, stream, ct);
        }

        try
        {
            MessageDto message = await messages.SendAsync(
                channelId,
                content ?? string.Empty,
                ct,
                attachmentUrl,
                Path.GetFileName(file.FileName),
                contentType,
                file.Length);

            return Results.Created($"/api/messages/{message.Id}", message);
        }
        catch
        {
            try
            {
                await storage.DeleteAsync(
                    attachmentUrl, CancellationToken.None);
            }
            catch
            {
            }

            throw;
        }
    }

    private static async Task<IResult> GetHistoryAsync(
        int channelId, int? beforeId, int? limit, 
        Messages messages, CancellationToken ct
    )
    {
        IReadOnlyList<MessageDto> result = await messages.GetHistoryAsync(
            channelId, beforeId, limit ?? 50, ct
        );

        return Results.Ok(result);
    }

    private static async Task<IResult> EditAsync(
        int messageId, EditMessageRequest request, 
        Messages messages, CancellationToken ct
    )
    {
        await messages.EditAsync(messageId, request.Content, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ToggleReactionAsync(
        int messageId,
        ToggleReactionRequest request,
        Messages messages,
        CancellationToken ct)
    {
        ReactionResultDto result = await messages.ToggleReactionAsync(
            messageId, request.Emoji, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteAsync(
        int messageId,
        Messages messages,
        IFileStorage storage,
        CancellationToken ct)
    {
        string? attachmentUrl = await messages.DeleteAsync(messageId, ct);

        try
        {
            await storage.DeleteAsync(attachmentUrl, ct);
        }
        catch
        {
        }

        return Results.NoContent();
    }

    public sealed record SendMessageRequest(
        string Content, int? ReplyToMessageId = null);
    public sealed record EditMessageRequest(string Content);
    public sealed record ToggleReactionRequest(string Emoji);
}
