using System.Security.Cryptography;
using System.Text;
using LedgerTransactionsAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LedgerTransactionsAPI.Middlewares;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IIdempotencyRepository repo, IUnitOfWork uow)
    {
        // Solo métodos con efectos
        if (context.Request.Method is not ("POST" or "PUT" or "PATCH"))
        {
            await _next(context);
            return;
        }

        var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
        {
            // Si quieres forzarlo, devuelve 400 aquí en lugar de dejar pasar
            await _next(context);
            return;
        }

        // Lee body para crear hash estable de la petición
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var raw = $"{context.Request.Method}|{context.Request.Path}|{body}";
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        // ¿Ya existe la clave?
        var existing = await repo.GetAsync(key, context.RequestAborted);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("""
                { "code":"IDEMPOTENCY_KEY_REUSED_DIFFERENT_REQUEST", "message":"The Idempotency-Key was already used with a different request body." }
                """);
                return;
            }

            // Misma key y mismo hash => devolver el resultado previo
            context.Response.StatusCode = existing.ResponseCode;
            if (!string.IsNullOrEmpty(existing.ResponseBody))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(existing.ResponseBody);
            }
            return;
        }

        // Capturar respuesta
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await _next(context); // deja ejecutar la acción

        // Lee el body resultante
        buffer.Position = 0;
        var responseBody = await new StreamReader(buffer).ReadToEndAsync();
        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody, context.RequestAborted);
        context.Response.Body = originalBody;

        // Persistir resultado para esta key
        var record = new Models.IdempotencyKey
        {
            Key = key,
            RequestHash = requestHash,
            ResponseCode = context.Response.StatusCode,
            ResponseBody = responseBody,
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(record, context.RequestAborted);
        await uow.SaveChangesAsync(context.RequestAborted);
    }
}

public static class IdempotencyExtensions
{
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
        => app.UseMiddleware<IdempotencyMiddleware>();
}
