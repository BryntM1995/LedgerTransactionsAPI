using LedgerTransactionsAPI.Dtos;
using LedgerTransactionsAPI.Services;
using LedgerTransactionsAPI.Swagger.LedgerTransactions.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace LedgerTransactionsAPI.Controllers;

[ApiController]
[Route("v1")]
public class AccountOperationController(ILedgerService svc) : ControllerBase
{
    private readonly ILedgerService _svc = svc;

    [HttpPost("create/account")]
    public Task<AccountResponse> CreateAccount([FromBody] CreateAccountRequest req, CancellationToken ct)
        => _svc.CreateAccountAsync(req, ct);

    [HttpGet("accounts/{id:guid}")]
    public async Task<IActionResult> GetAccount(Guid id, CancellationToken ct)
        => (await _svc.GetAccountAsync(id, ct)) is { } acc ? Ok(acc) : NotFound(new { code = "ACCOUNT_NOT_FOUND" });

    [Idempotent]
    [HttpPost("accounts/{id:guid}/deposits")]
    public async Task<IActionResult> Deposit(Guid id, [FromBody] DepositWithdrawRequest req, CancellationToken ct)
        => Ok(new { transactionId = await _svc.DepositAsync(id, req.Amount, req.Description, ct) });

    [Idempotent]
    [HttpPost("accounts/{id:guid}/withdrawals")]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] DepositWithdrawRequest req, CancellationToken ct)
    {
        try
        {
            var tid = await _svc.WithdrawAsync(id, req.Amount, req.Description, ct);
            return Ok(new { transactionId = tid });
        }
        catch (InvalidOperationException ex) when (ex.Message == "INSUFFICIENT_FUNDS")
        {
            return BadRequest(new { code = "INSUFFICIENT_FUNDS", message = "Not enough balance." });
        }
    }
    [Idempotent]
    [HttpPost("transfers")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest req, CancellationToken ct)
    {
        try
        {
            var tid = await _svc.TransferAsync(req, ct);
            return Ok(new { transactionId = tid });
        }
        catch (InvalidOperationException ex) when (ex.Message == "SAME_ACCOUNT")
        {
            return UnprocessableEntity(new { code = "SAME_ACCOUNT", message = "Source and target must be different." });
        }
        catch (InvalidOperationException ex) when (ex.Message == "SOURCE_CURRENCY_MISMATCH")
        {
            return UnprocessableEntity(new { code = "SOURCE_CURRENCY_MISMATCH", message = "Currency mismatch with source account." });
        }
        catch (InvalidOperationException ex) when (ex.Message == "INSUFFICIENT_FUNDS")
        {
            return BadRequest(new { code = "INSUFFICIENT_FUNDS", message = "Not enough balance." });
        }
    }
}
