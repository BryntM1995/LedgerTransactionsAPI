namespace LedgerTransactionsAPI.Dtos;

public record CreateAccountRequest(string Holder, string Currency, decimal? InitialBalance);
public record AccountResponse(Guid Id, string Holder, string Currency, decimal AvailableBalance, DateTime CreatedAt);
