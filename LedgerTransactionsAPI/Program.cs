using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Middlewares;
using LedgerTransactionsAPI.Outbox;
using LedgerTransactionsAPI.Repositories.Ef;
using LedgerTransactionsAPI.Repositories.Interfaces;
using LedgerTransactionsAPI.Services;
using LedgerTransactionsAPI.Swagger;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<LedgerDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
);

// Repos + UoW
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ILedgerEntryRepository, LedgerEntryRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
builder.Services.AddScoped<ITransactionReadRepository, TransactionReadRepository>();
builder.Services.AddScoped<ILedgerReadRepository, LedgerReadRepository>();

// Services
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddScoped<IReadService, ReadService>();
builder.Services.AddScoped<ITransactionReadRepository, TransactionReadRepository>();
builder.Services.AddScoped<ILedgerReadRepository, LedgerReadRepository>();

// Outbox (config -> httpclient -> worker)
builder.Services.Configure<OutboxOptions>(
    builder.Configuration.GetSection("Outbox"));

builder.Services.AddHttpClient("outbox");

builder.Services.AddHostedService<OutboxPublisher>();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<IdempotencyKeyHeaderFilter>(); // Header en endpoints marcados con [Idempotent]
});

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Idempotencia antes de Authorization para cachear respuestas repetidas
app.UseIdempotency();

app.UseAuthorization();

app.MapControllers();

app.Run();
