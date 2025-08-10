// Repositories/Interfaces/ITransactionReadRepository.cs
using LedgerTransactionsAPI.Dtos;

namespace LedgerTransactionsAPI.Repositories.Interfaces;
public enum PageDirection { Next, Prev }

public interface ITransactionReadRepository
{
    Task<PagedResult<TransactionItem>> ListByAccountAsync(
        Guid accountId, int limit, string? cursor, PageDirection direction, CancellationToken ct);
}
