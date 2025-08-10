// Controllers/LedgerQueryController.cs
using LedgerTransactionsAPI.Dtos;
using LedgerTransactionsAPI.Repositories.Interfaces; // PageDirection
using LedgerTransactionsAPI.Services;                // IReadService
using Microsoft.AspNetCore.Mvc;

namespace LedgerTransactionsAPI.Controllers;

[ApiController]
[Route("v1/ledger")]
public class LedgerQueryController : ControllerBase
{
    private readonly IReadService _read;
    public LedgerQueryController(IReadService read) => _read = read;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LedgerEntryItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LedgerEntryItem>>> Get(
        [FromQuery] Guid? accountId,
        [FromQuery] int limit = 50,
        [FromQuery] string? cursor = null,
        [FromQuery] string direction = "next",
        CancellationToken ct = default)
    {
        var dir = direction.Equals("prev", StringComparison.OrdinalIgnoreCase)
            ? PageDirection.Prev
            : PageDirection.Next;

        limit = Math.Clamp(limit, 1, 200);

        var page = await _read.GetLedgerAsync(accountId, limit, cursor, dir, ct);
        return Ok(page);
    }
}
