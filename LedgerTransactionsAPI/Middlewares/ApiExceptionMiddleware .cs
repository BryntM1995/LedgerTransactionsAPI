using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LedgerTransactionsAPI.Middlewares;

public class ApiExceptionMiddleware : IMiddleware
{
    private readonly ILogger<ApiExceptionMiddleware> _log;
    public ApiExceptionMiddleware(ILogger<ApiExceptionMiddleware> log) => _log = log;

    private sealed record ErrorBody(string code, string message);

    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        try
        {
            await next(ctx);
        }
        catch (KeyNotFoundException ex)
        {
            await Write(ctx, HttpStatusCode.NotFound, "NOT_FOUND", ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message == "INSUFFICIENT_FUNDS")
        {
            await Write(ctx, (HttpStatusCode)422, "INSUFFICIENT_FUNDS", "Not enough funds.");
        }
        catch (InvalidOperationException ex) when (ex.Message == "SAME_ACCOUNT")
        {
            await Write(ctx, (HttpStatusCode)422, "SAME_ACCOUNT", "Source and target accounts must differ.");
        }
        catch (InvalidOperationException ex) when (ex.Message == "SOURCE_CURRENCY_MISMATCH")
        {
            await Write(ctx, (HttpStatusCode)422, "SOURCE_CURRENCY_MISMATCH", "Currency in request must match source account.");
        }
        catch (ArgumentException ex)
        {
            await Write(ctx, (HttpStatusCode)422, "VALIDATION_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled error");
            await Write(ctx, HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "Unexpected error.");
        }
    }

    private static async Task Write(HttpContext ctx, HttpStatusCode status, string code, string message)
    {
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(new ErrorBody(code, message));
        await ctx.Response.WriteAsync(payload);
    }
}
