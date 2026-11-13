using Qwek.Domains.Entities;

namespace Qwek.Domains.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(int id, CancellationToken ct); 
    Task<Message?> GetByAttachmentUrlAsync(
        string attachmentUrl, CancellationToken ct);
    Task<IReadOnlyList<Message>> GetByIdsAsync(
        IReadOnlyCollection<int> ids, CancellationToken ct);
    Task<IReadOnlyList<Message>> GetPageAsync(int channelId, int? beforeId, int limit, CancellationToken ct);
    Task AddAsync(Message message, CancellationToken ct); 
    void Remove(Message message);
}
