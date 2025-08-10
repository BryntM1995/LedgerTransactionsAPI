using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Middlewares;
using LedgerTransactionsAPI.Outbox;
using LedgerTransactionsAPI.Repositories.Ef;
using LedgerTransactionsAPI.Repositories.Interfaces;
using LedgerTransactionsAPI.Services;
using LedgerTransactionsAPI.Swagger;
using LedgerTransactionsAPI.Swagger.LedgerTransactions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ILedgerEntryRepository, LedgerEntryRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));
builder.Services.AddHttpClient("outbox"); // HttpClient para publicar
builder.Services.AddHostedService<OutboxPublisher>();

// DbContext
builder.Services.AddDbContext<LedgerDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<IdempotencyKeyHeaderFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseIdempotency();
app.MapControllers();

app.Run();
