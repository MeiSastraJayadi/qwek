using RealTimeApps.Applications.Ports;
using RealTimeApps.Infrastructures.Persistence;

namespace RealTimeApps.Infrastructures.Repositories;

public class UnitOfWorks : IUnitOfWork
{
    private readonly AppDbContext _db; 

    public UnitOfWorks(AppDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct) => 
        _db.SaveChangesAsync(ct);
}
