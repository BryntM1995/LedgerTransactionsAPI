using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Middlewares;
using LedgerTransactionsAPI.Outbox;
using LedgerTransactionsAPI.Repositories.Ef;
using LedgerTransactionsAPI.Repositories.Interfaces;
using LedgerTransactionsAPI.Services;
using LedgerTransactionsAPI.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

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

//jwt
var jwt = builder.Configuration.GetSection("Jwt");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AuditorOnly", p => p.RequireRole("Auditor"));
    o.AddPolicy("AnyUser", p => p.RequireRole("Teller", "Auditor"));
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        // prioridad: sub del JWT; fallback: X-Api-Key; luego IP
        var sub = ctx.User?.FindFirst("sub")?.Value;
        var keyHdr = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
        var partitionKey = sub ?? keyHdr ?? ip;

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<IdempotencyKeyHeaderFilter>();

    // 🔹 Configuración JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header usando el esquema Bearer.  
                        Ejemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
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

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();
