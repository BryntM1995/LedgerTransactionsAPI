using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LedgerTransactionsAPI.Controllers;

[ApiController]
[Route("v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly ILogger<WebhooksController> _logger;
    public WebhooksController(ILogger<WebhooksController> logger) => _logger = logger;

    [HttpPost("test")]
    public IActionResult Receive([FromBody] object payload)
    {
        _logger.LogInformation("Webhook received: {Payload}", System.Text.Json.JsonSerializer.Serialize(payload));
        return Ok(new { status = "ok" });
    }
}
