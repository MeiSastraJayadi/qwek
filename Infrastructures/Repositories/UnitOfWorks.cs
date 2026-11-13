using Qwek.Applications.Ports;
using Qwek.Infrastructures.Persistence;

namespace Qwek.Infrastructures.Repositories;

public class UnitOfWorks : IUnitOfWork
{
    private readonly AppDbContext _db; 

    public UnitOfWorks(AppDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct) => 
        _db.SaveChangesAsync(ct);
}
