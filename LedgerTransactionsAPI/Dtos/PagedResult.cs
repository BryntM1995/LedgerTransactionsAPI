// Dtos/PagedResult.cs
namespace LedgerTransactionsAPI.Dtos;
public record PagedResult<T>(IReadOnlyList<T> Items, string? NextCursor, string? PrevCursor);
