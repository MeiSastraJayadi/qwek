namespace Qwek.Domains.Entities;

public class Message
{
    public int Id { get; private set; }
    public string MessageContent { get; private set; } = string.Empty;
    public int ChannelId { get; private set; }
    public string PersonalUserId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public string? AttachmentUrl { get; private set; }
    public string? AttachmentName { get; private set; }
    public string? AttachmentContentType { get; private set; }
    public long? AttachmentSize { get; private set; }
    public int? ReplyToMessageId { get; private set; }

    private Message() {}
    public Message(
        string message,
        int channelId,
        string personalUserId,
        DateTime? createdAt,
        string? attachmentUrl = null,
        string? attachmentName = null,
        string? attachmentContentType = null,
        long? attachmentSize = null,
        int? replyToMessageId = null)
    {
        SetContent(message, allowEmpty: attachmentUrl is not null);
        ChannelId = channelId;
        PersonalUserId = personalUserId;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        AttachmentUrl = attachmentUrl;
        AttachmentName = attachmentName;
        AttachmentContentType = attachmentContentType;
        AttachmentSize = attachmentSize;
        ReplyToMessageId = replyToMessageId;
    }

    public void Edit(string content)
    {
        SetContent(content); 
        EditedAt = DateTime.UtcNow;
    }

    private void SetContent(string content, bool allowEmpty = false)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            if (!allowEmpty)
                throw new ArgumentException("Message content cannot be empty");

            MessageContent = string.Empty;
            return;
        }
        
        MessageContent = content.Trim();
    }
}
