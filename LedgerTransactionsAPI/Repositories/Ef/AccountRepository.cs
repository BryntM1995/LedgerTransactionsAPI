using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Models;
using LedgerTransactionsAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LedgerTransactionsAPI.Repositories.Ef;

public class AccountRepository : IAccountRepository
{
    private readonly LedgerDbContext _db;
    public AccountRepository(LedgerDbContext db) => _db = db;

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Account?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
        => await  _db.Accounts
              .FromSqlRaw(@"SELECT * FROM accounts WHERE ""Id"" = {0} FOR UPDATE", id)
              .AsTracking()
              .FirstOrDefaultAsync(ct);

    public async Task<(Account source, Account target)> GetPairForUpdateAsync(Guid sourceId, Guid targetId, CancellationToken ct = default)
    {
        var firstId = sourceId.CompareTo(targetId) <= 0 ? sourceId : targetId;
        var secondId = firstId == sourceId ? targetId : sourceId;

        var list = await _db.Accounts
            .FromSqlRaw(@"SELECT * FROM accounts WHERE ""Id"" IN ({0}, {1}) ORDER BY ""Id"" FOR UPDATE", firstId, secondId)
            .AsTracking()
            .ToListAsync(ct);

        if (list.Count != 2) throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");

        var first = list[0];
        var second = list[1];

        var source = first.Id == sourceId ? first : second;
        var target = first.Id == targetId ? first : second;

        return (source, target);
    }

    public async Task AddAsync(Account account, CancellationToken ct = default)
    {
        await _db.Accounts.AddAsync(account);
    }

    public void Update(Account account) => _db.Accounts.Update(account);
}
