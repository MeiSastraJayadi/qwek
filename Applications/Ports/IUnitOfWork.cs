namespace Qwek.Applications.Ports;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}
