namespace LedgerTransactionsAPI.Dtos;

public record LedgerEntryItem(
    Guid Id,
    Guid TransactionId,
    Guid AccountId,
    decimal Debit,
    decimal Credit,
    string Currency,
    DateTime CreatedAt
);
