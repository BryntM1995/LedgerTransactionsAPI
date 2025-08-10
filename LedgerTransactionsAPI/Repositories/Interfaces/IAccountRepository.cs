using LedgerTransactionsAPI.Models;

namespace LedgerTransactionsAPI.Repositories.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    void Update(Account account);
}
