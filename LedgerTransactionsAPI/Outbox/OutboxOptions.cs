
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
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly OutboxOptions _opts;
    private readonly HttpClient _http;

    public OutboxPublisher(IServiceProvider sp, ILogger<OutboxPublisher> logger, IOptions<OutboxOptions> opts, IHttpClientFactory httpFactory)
    {
        _sp = sp;
        _logger = logger;
        _opts = opts.Value;
        _http = httpFactory.CreateClient("outbox");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher started. Interval={Interval}s, Batch={Batch}", _opts.PollingIntervalSeconds, _opts.BatchSize);

        var delay = TimeSpan.FromSeconds(Math.Max(1, _opts.PollingIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                if (processed == 0)
                    await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxPublisher loop error");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LedgerTransactionsAPI.Data.LedgerDbContext>();

        // No publicaremos si no hay URL configurada (solo log)
        var hasWebhook = !string.IsNullOrWhiteSpace(_opts.WebhookUrl);

        // Transacción para tomar en exclusión los eventos
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Selecciona lote con lock y evita competir con otros workers
        var events = await db.DomainOutbox
            .FromSqlRaw(
                "SELECT * FROM domain_outbox WHERE published = false ORDER BY created_at LIMIT {0} FOR UPDATE SKIP LOCKED",
                _opts.BatchSize)
            .AsTracking()
            .ToListAsync(ct);

        if (events.Count == 0)
        {
            await tx.RollbackAsync(ct);
            return 0;
        }

        _logger.LogInformation("Fetched {Count} outbox events", events.Count);

        foreach (var ev in events)
        {
            try
            {
                if (hasWebhook)
                {
                    // Publicación idempotente: el consumidor debe tratar event.Id como idempotency key
                    var payload = new
                    {
                        id = ev.Id,
                        type = ev.Type,
                        occurredAt = ev.CreatedAt,
                        data = System.Text.Json.JsonSerializer.Deserialize<object>(ev.Payload)
                    };

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var resp = await _http.PostAsJsonAsync(_opts.WebhookUrl!, payload, cts.Token);
                    resp.EnsureSuccessStatusCode();
                }
                else
                {
                    _logger.LogWarning("No Outbox.WebhookUrl configured. Event {Id} would be sent now.", ev.Id);
                }

                // Marcar publicado
                ev.Published = true;
                ev.PublishedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                // No marcamos publicado si falló; dejamos para reintento en próxima pasada
                _logger.LogError(ex, "Failed to publish outbox event {Id}", ev.Id);
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return events.Count(e => e.Published);
    }
}
