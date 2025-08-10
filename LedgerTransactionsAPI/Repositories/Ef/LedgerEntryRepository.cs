using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Models;
using LedgerTransactionsAPI.Repositories.Interfaces;

namespace LedgerTransactionsAPI.Repositories.Ef;

public class LedgerEntryRepository : ILedgerEntryRepository
{
    private readonly LedgerDbContext _db;
    public LedgerEntryRepository(LedgerDbContext db) => _db = db;

    public async Task AddAsync(LedgerEntry entry, CancellationToken ct = default)
    {
        await _db.LedgerEntries.AddAsync(entry);
    }

    public async Task AddRangeAsync(IEnumerable<LedgerEntry> entries, CancellationToken ct = default)
    {
        await _db.LedgerEntries.AddRangeAsync(entries);
    }
}
