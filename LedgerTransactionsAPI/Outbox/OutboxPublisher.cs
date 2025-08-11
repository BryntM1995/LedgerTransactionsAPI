using System.Net.Http.Json;
using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LedgerTransactionsAPI.Outbox;

public class OutboxOptions
{
    public int PollingIntervalSeconds { get; set; } = 2;
    public int BatchSize { get; set; } = 50;
    public string? WebhookUrl { get; set; }
}

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<OutboxPublisher> _log;
    private readonly OutboxOptions _opts;
    private readonly HttpClient _http;

    public OutboxPublisher(IServiceProvider sp, ILogger<OutboxPublisher> log, IOptions<OutboxOptions> opts, IHttpClientFactory httpFactory)
    {
        _sp = sp;
        _log = log;
        _opts = opts.Value;
        _http = httpFactory.CreateClient("outbox");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(1, _opts.PollingIntervalSeconds));
        _log.LogInformation("OutboxPublisher started: interval={Interval}s batch={Batch}", _opts.PollingIntervalSeconds, _opts.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sent = await PublishBatchAsync(stoppingToken);
                if (sent == 0) await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error in OutboxPublisher loop");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task<int> PublishBatchAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var events = await db.DomainOutbox
            .FromSqlRaw(@"
            SELECT ""Id"", ""Type"", ""Payload"", ""CreatedAt"", ""Published"", ""PublishedAt""
            FROM domain_outbox
            WHERE ""Published"" = FALSE
            ORDER BY ""CreatedAt""
            LIMIT {0} FOR UPDATE SKIP LOCKED", _opts.BatchSize)
            .AsTracking()
            .ToListAsync(ct);

        _log.LogInformation("Outbox: fetched {Count} events", events.Count);

        if (events.Count == 0)
        {
            await tx.RollbackAsync(ct);
            return 0;
        }

        var hasWebhook = !string.IsNullOrWhiteSpace(_opts.WebhookUrl);
        var sent = 0;

        foreach (var ev in events)
        {
            try
            {
                if (hasWebhook)
                {
                    var payload = new
                    {
                        id = ev.Id,
                        type = ev.Type,
                        occurredAt = ev.CreatedAt,
                        data = System.Text.Json.JsonSerializer.Deserialize<object>(ev.Payload)
                    };

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    _log.LogInformation("Outbox: posting {Id} -> {Url}", ev.Id, _opts.WebhookUrl);
                    var resp = await _http.PostAsJsonAsync(_opts.WebhookUrl!, payload, cts.Token);
                    _log.LogInformation("Outbox: response {StatusCode}", resp.StatusCode);
                    resp.EnsureSuccessStatusCode();
                }
                else
                {
                    _log.LogWarning("Outbox: no WebhookUrl configured. Event {Id} would be sent.", ev.Id);
                }

                // UPDATE directo (evita cualquier tema de tracking/mapeo)
                var affected = await db.Database.ExecuteSqlRawAsync(@"
                UPDATE domain_outbox
                SET ""Published"" = TRUE,
                    ""PublishedAt"" = NOW()
                WHERE ""Id"" = {0}", ev.Id);

                _log.LogInformation("Outbox: event {Id} marked published (affected={Affected})", ev.Id, affected);
                sent++;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Outbox: failed to publish event {Id}", ev.Id);
                // No se marca publicado; quedará para reintento en próximo ciclo
            }
        }

        await tx.CommitAsync(ct);
        return sent;
    }

}
