using LedgerTransactionsAPI.Models;

namespace LedgerTransactionsAPI.Repositories.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(DomainEvent ev, CancellationToken ct = default);
}
