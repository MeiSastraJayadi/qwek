namespace Qwek.Domains.Entities;

public class MessageReaction
{
    public int Id { get; private set; }
    public int MessageId { get; private set; }
    public string PersonalUserId { get; private set; } = string.Empty;
    public string Emoji { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private MessageReaction() {}

    public MessageReaction(
        int messageId,
        string personalUserId,
        string emoji,
        DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(emoji))
        {
            throw new ArgumentException("Reaction emoji is required", nameof(emoji));
        }

        string trimmed = emoji.Trim();

        if (trimmed.Length > 32)
        {
            throw new ArgumentException("Reaction emoji is too long", nameof(emoji));
        }

        MessageId = messageId;
        PersonalUserId = personalUserId;
        Emoji = trimmed;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
