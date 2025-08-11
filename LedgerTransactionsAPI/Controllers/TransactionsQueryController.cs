// Controllers/TransactionsQueryController.cs
using LedgerTransactionsAPI.Dtos;
using LedgerTransactionsAPI.Repositories.Interfaces; // PageDirection
using LedgerTransactionsAPI.Services;                // IReadService
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerTransactionsAPI.Controllers;

[ApiController]
[Route("v1/accounts/{id:guid}/transactions")]
public class TransactionsQueryController : ControllerBase
{
    private readonly IReadService _read;
    public TransactionsQueryController(IReadService read) => _read = read;

    /// Lists account transactions with keyset pagination.
    [Authorize(Policy = "AuditorOnly")]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TransactionItem>>> Get(
        Guid id,
        [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null,
        [FromQuery] string direction = "next",
        CancellationToken ct = default)
    {
        var isAuditor = User.IsInRole("Auditor");
        if (!isAuditor)
        {
                // El reto debería solicitar la implementación de  asignacion de un rol por cuenta creada
                // si un customer hace la solicitud de ver las transferencias se filtraria por el id de su cuenta
                return Forbid(); // 403
        }

        var dir = direction.Equals("prev", StringComparison.OrdinalIgnoreCase)
            ? PageDirection.Prev : PageDirection.Next;

        limit = Math.Clamp(limit, 1, 100);
        var page = await _read.GetAccountTransactionsAsync(id, limit, cursor, dir, ct);
        return Ok(page);
    }
}
