// Repositories/Interfaces/ILedgerReadRepository.cs
using LedgerTransactionsAPI.Dtos;

namespace LedgerTransactionsAPI.Repositories.Interfaces;

public interface ILedgerReadRepository
{
    Task<PagedResult<LedgerEntryItem>> ListAsync(
        Guid? accountId, int limit, string? cursor, PageDirection direction, CancellationToken ct);
}