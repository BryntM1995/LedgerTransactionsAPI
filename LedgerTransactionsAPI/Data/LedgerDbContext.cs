using LedgerTransactionsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LedgerTransactionsAPI.Data
{
    public class LedgerDbContext : DbContext
    {
        public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options) { }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<LedgerTransaction> Transactions => Set<LedgerTransaction>();
        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
        public DbSet<DomainEvent> DomainOutbox => Set<DomainEvent>();
        public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

        protected override void OnModelCreating(ModelBuilder model)
        {
            // accounts
            model.Entity<Account>(e =>
            {
                e.ToTable("accounts");
                e.HasKey(x => x.Id);
                e.Property(x => x.Holder).IsRequired();
                e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
                e.Property(x => x.AvailableBalance).HasColumnType("numeric(18,2)");
                e.Property(x => x.Version).IsConcurrencyToken(); 
                e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
                e.HasIndex(x => new { x.CreatedAt, x.Id });
            });

            // transactions
            model.Entity<LedgerTransaction>(e =>
            {
                e.ToTable("transactions");
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).IsRequired();
                e.Property(x => x.Amount).HasColumnType("numeric(18,2)");
                e.Property(x => x.Date).HasColumnType("timestamptz");
                e.HasIndex(x => new { x.Date, x.Id }).HasDatabaseName("ix_transactions_date_id");
                e.Property(x => x.FxPair).HasMaxLength(7);         
                e.Property(x => x.FxRate).HasColumnType("numeric(18,6)");
            });

            // ledger_entries (doble partida)
            model.Entity<LedgerEntry>(e =>
            {
                e.ToTable("ledger_entries");
                e.HasKey(x => x.Id);
                e.Property(x => x.Debit).HasColumnType("numeric(18,2)");
                e.Property(x => x.Credit).HasColumnType("numeric(18,2)");
                e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
                e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
                e.HasIndex(x => new { x.CreatedAt, x.Id }).HasDatabaseName("ix_ledger_entries_created_at_id");
                e.Property(x => x.BaseCurrency).HasMaxLength(3);
                e.Property(x => x.BaseDebit).HasColumnType("numeric(18,2)");
                e.Property(x => x.BaseCredit).HasColumnType("numeric(18,2)");
                e.Property(x => x.FxRate).HasColumnType("numeric(18,6)");
            });

            // idempotency_keys
            model.Entity<IdempotencyKey>(e =>
            {
                e.ToTable("idempotency_keys");
                e.HasKey(x => x.Key);
                e.Property(x => x.RequestHash).HasMaxLength(512);
                e.Property(x => x.ResponseBody).HasColumnType("jsonb");
                e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
            });

            // domain_outbox
            model.Entity<DomainEvent>(e =>
            {
                e.ToTable("domain_outbox");
                e.HasKey(x => x.Id);
                e.Property(x => x.Type).IsRequired();
                e.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
                e.Property(x => x.CreatedAt).HasColumnType("timestamptz");
                e.Property(x => x.PublishedAt).HasColumnType("timestamptz");
                e.Property(x => x.Published).HasDefaultValue(false);
                e.HasIndex(x => new { x.Published, x.CreatedAt });
            });

            // --- Seed mínimo (IDs fijos para pruebas) ---
            var accA = new Account { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Holder = "Cuenta A", Currency = "DOP", AvailableBalance = 10000m, Version = 1, CreatedAt = DateTime.UtcNow };
            var accB = new Account { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Holder = "Cuenta B", Currency = "DOP", AvailableBalance = 2500m, Version = 1, CreatedAt = DateTime.UtcNow };
            var accC = new Account { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Holder = "Cuenta C", Currency = "USD", AvailableBalance = 100m, Version = 1, CreatedAt = DateTime.UtcNow };

            // 🔹 Cuenta interna para redondeo (moneda base DOP)
            var accRounding = new Account
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                Holder = "FX_ROUNDING",
                Currency = "DOP",
                AvailableBalance = 0m,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };

            model.Entity<Account>().HasData(accA, accB, accC, accRounding);
        }
    }
}
