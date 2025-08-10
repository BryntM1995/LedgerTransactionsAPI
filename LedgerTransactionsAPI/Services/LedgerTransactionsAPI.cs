// Services/IReadService.cs
using LedgerTransactionsAPI.Dtos;
using LedgerTransactionsAPI.Repositories.Interfaces; // PageDirection

namespace LedgerTransactionsAPI.Services;

public interface IReadService
{
    Task<PagedResult<TransactionItem>> GetAccountTransactionsAsync(
        Guid accountId, int limit, string? cursor, PageDirection direction = PageDirection.Next, CancellationToken ct = default);

    Task<PagedResult<LedgerEntryItem>> GetLedgerAsync(
        Guid? accountId, int limit, string? cursor, PageDirection direction = PageDirection.Next, CancellationToken ct = default);
}
