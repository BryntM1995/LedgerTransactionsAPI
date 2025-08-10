namespace LedgerTransactionsAPI.Dtos;

public record DepositWithdrawRequest(decimal Amount, string? Description);
public record TransferRequest(Guid SourceAccountId, Guid TargetAccountId, decimal Amount, string Currency, string? Description);
public record TransferResponse(Guid TransactionId, Guid SourceAccountId, Guid TargetAccountId, decimal Amount, string Currency);
