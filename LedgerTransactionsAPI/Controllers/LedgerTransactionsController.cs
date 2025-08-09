using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class LedgerTransactionsAPIController : ControllerBase
{
    private readonly ILedgerService _ledgerService;

    public LedgerTransactionsAPIController(ILedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    [HttpGet("{ledgerId}")]
    public async Task<IActionResult> GetLedgerTransactions(int ledgerId)
    {
        var transactions = await _ledgerService.GetLedgerTransactionsAsync(ledgerId);

        if (transactions == null || transactions.Count == 0)
            return NotFound(new { message = "No se encontraron transacciones para este ledger." });

        return Ok(transactions);
    }
}
