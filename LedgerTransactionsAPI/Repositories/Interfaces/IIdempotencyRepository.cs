using LedgerTransactionsAPI.Models;

namespace LedgerTransactionsAPI.Repositories.Interfaces;

public interface IIdempotencyRepository
{
    Task<IdempotencyKey?> GetAsync(string key, CancellationToken ct = default);
    Task AddAsync(IdempotencyKey key, CancellationToken ct = default);
    Task UpdateAsync(IdempotencyKey key, CancellationToken ct = default);
}
