// Controllers/TransactionsQueryController.cs
using LedgerTransactionsAPI.Dtos;
using LedgerTransactionsAPI.Repositories.Interfaces; // PageDirection enum
using LedgerTransactionsAPI.Services;               // IReadService
using Microsoft.AspNetCore.Mvc;

namespace LedgerTransactionsAPI.Controllers;

[ApiController]
[Route("v1/accounts/{id:guid}/transactions")]
public class TransactionsQueryController : ControllerBase
{
    private readonly IReadService _read;
    public TransactionsQueryController(IReadService read) => _read = read;

    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionItem>>> Get(
        Guid id,
        [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null,
        [FromQuery] string direction = "next",
        CancellationToken ct = default)
    {
        var dir = direction.Equals("prev", StringComparison.OrdinalIgnoreCase)
            ? PageDirection.Prev
            : PageDirection.Next;

        limit = Math.Clamp(limit, 1, 100);

        var page = await _read.GetAccountTransactionsAsync(id, limit, cursor, dir, ct);
        return Ok(page);
    }
}
