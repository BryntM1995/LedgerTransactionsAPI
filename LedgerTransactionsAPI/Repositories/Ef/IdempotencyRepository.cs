using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Models;
using LedgerTransactionsAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LedgerTransactionsAPI.Repositories.Ef;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly LedgerDbContext _db;
    public IdempotencyRepository(LedgerDbContext db) => _db = db;

    public async Task<IdempotencyKey?> GetAsync(string key, CancellationToken ct = default)
        => await _db.IdempotencyKeys.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct);

    public async Task AddAsync(IdempotencyKey key, CancellationToken ct = default)
    {
        await _db.IdempotencyKeys.AddAsync(key);
    }

    public Task UpdateAsync(IdempotencyKey key, CancellationToken ct = default)
    {
        _db.IdempotencyKeys.Update(key);
        return Task.CompletedTask;
    }
}
