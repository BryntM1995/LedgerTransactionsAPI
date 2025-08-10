using LedgerTransactionsAPI.Dtos;

namespace LedgerTransactionsAPI.Services;

public interface ILedgerService
{
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest req, CancellationToken ct = default);
    Task<AccountResponse?> GetAccountAsync(Guid id, CancellationToken ct = default);

    Task<Guid> DepositAsync(Guid accountId, decimal amount, string? description, CancellationToken ct = default);
    Task<Guid> WithdrawAsync(Guid accountId, decimal amount, string? description, CancellationToken ct = default);
    Task<Guid> TransferAsync(TransferRequest req, CancellationToken ct = default);
}
