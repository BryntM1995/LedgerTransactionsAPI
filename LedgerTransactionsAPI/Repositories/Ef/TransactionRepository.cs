using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Models;
using LedgerTransactionsAPI.Repositories.Interfaces;

namespace LedgerTransactionsAPI.Repositories.Ef;

public class TransactionRepository : ITransactionRepository
{
    private readonly LedgerDbContext _db;
    public TransactionRepository(LedgerDbContext db) => _db = db;

    public async Task AddAsync(LedgerTransaction tx, CancellationToken ct = default)
    {
        await _db.Transactions.AddAsync(tx);
    }
    public async Task AddRangeAsync(IEnumerable<LedgerTransaction> entries, CancellationToken ct = default)
    {
        await _db.Transactions.AddRangeAsync(entries);
    }
}
