using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Models;
using LedgerTransactionsAPI.Repositories.Interfaces;

namespace LedgerTransactionsAPI.Repositories.Ef;

public class OutboxRepository : IOutboxRepository
{
    private readonly LedgerDbContext _db;
    public OutboxRepository(LedgerDbContext db) => _db = db;

    public async Task AddAsync(DomainEvent ev, CancellationToken ct = default)
    {
        await _db.DomainOutbox.AddAsync(ev);
    }
    public async Task AddRangeAsync(IEnumerable<DomainEvent> entries, CancellationToken ct = default)
    {
        await _db.DomainOutbox.AddRangeAsync(entries);
    }
}
