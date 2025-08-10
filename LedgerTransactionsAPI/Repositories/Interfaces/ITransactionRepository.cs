using LedgerTransactionsAPI.Models;

namespace LedgerTransactionsAPI.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task AddAsync(LedgerTransaction tx, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<LedgerTransaction> entries, CancellationToken ct = default);
}
