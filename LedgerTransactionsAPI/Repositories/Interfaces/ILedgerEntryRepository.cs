using LedgerTransactionsAPI.Models;

namespace LedgerTransactionsAPI.Repositories.Interfaces;

public interface ILedgerEntryRepository
{
    Task AddAsync(LedgerEntry entry, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<LedgerEntry> entries, CancellationToken ct = default);
}
