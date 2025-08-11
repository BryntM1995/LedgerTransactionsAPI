// Services/ReadService.cs
using LedgerTransactionsAPI.Dtos;
using LedgerTransactionsAPI.Repositories.Interfaces;

namespace LedgerTransactionsAPI.Services;

public class ReadService : IReadService
{
    private readonly ITransactionReadRepository _txRead;
    private readonly ILedgerReadRepository _ledgerRead;

    public ReadService(ITransactionReadRepository txRead, ILedgerReadRepository ledgerRead)
    {
        _txRead = txRead;
        _ledgerRead = ledgerRead;
    }

    public async Task<PagedResult<TransactionItem>> GetAccountTransactionsAsync(
        Guid accountId, int limit, string? cursor, PageDirection direction = PageDirection.Next, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 100);

        var page = await _txRead.ListByAccountAsync(accountId, limit, cursor, direction, ct);
        return page;
    }

    public async Task<PagedResult<LedgerEntryItem>> GetLedgerAsync(
        Guid? accountId, int limit, string? cursor, PageDirection direction = PageDirection.Next, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 200);
        return await _ledgerRead.ListAsync(accountId, limit, cursor, direction, ct);
    }
}
