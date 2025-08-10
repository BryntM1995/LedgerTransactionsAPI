using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace LedgerTransactionsAPI.Repositories.Ef;

public class UnitOfWork : IUnitOfWork
{
    private readonly LedgerDbContext _db;
    public UnitOfWork(LedgerDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) => await _db.SaveChangesAsync(ct);
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default) => await _db.Database.BeginTransactionAsync(ct);
}
