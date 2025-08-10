using Microsoft.AspNetCore.Mvc;

namespace LedgerTransactionsAPI.Controllers;

[ApiController]
[Route("v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly ILogger<WebhooksController> _log;
    public WebhooksController(ILogger<WebhooksController> log) => _log = log;

    [HttpPost("test")]
    public IActionResult Receive([FromBody] object payload)
    {
        _log.LogInformation("Webhook received: {Payload}", System.Text.Json.JsonSerializer.Serialize(payload));
        return Ok(new { status = "ok" });
    }
}
