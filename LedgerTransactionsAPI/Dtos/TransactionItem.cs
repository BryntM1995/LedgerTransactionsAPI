namespace LedgerTransactionsAPI.Dtos;

public record TransactionItem(
    Guid Id,
    string Type,
    string? Description,
    DateTime Date,
    decimal Amount,
    string Currency
);
