using LedgerTransactionsAPI.Dtos;
using LedgerTransactionsAPI.Models;
using LedgerTransactionsAPI.Repositories.Interfaces;

namespace LedgerTransactionsAPI.Services;

public class LedgerService : ILedgerService
{
    private readonly IUnitOfWork _uow;
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _txs;
    private readonly ILedgerEntryRepository _entries;
    private readonly IOutboxRepository _outbox;
    private readonly IFxRates _fx;

    public LedgerService(
        IUnitOfWork uow,
        IAccountRepository accounts,
        ITransactionRepository txs,
        ILedgerEntryRepository entries,
        IOutboxRepository outbox,
        IFxRates fx)
        
    {
        _uow = uow;
        _accounts = accounts;
        _txs = txs;
        _entries = entries;
        _outbox = outbox;
        _fx = fx;
    }

    public async Task<AccountResponse> CreateAccountAsync(CreateAccountRequest req, CancellationToken ct = default)
    {
        var acc = new Account
        {
            Id = Guid.NewGuid(),
            Holder = req.Holder,
            Currency = req.Currency.ToUpperInvariant(),
            AvailableBalance = req.InitialBalance.GetValueOrDefault(0m),
            CreatedAt = DateTime.UtcNow
        };
        await _accounts.AddAsync(acc, ct);
        await _uow.SaveChangesAsync(ct);
        return new AccountResponse(acc.Id, acc.Holder, acc.Currency, acc.AvailableBalance, acc.CreatedAt);
    }

    public async Task<AccountResponse?> GetAccountAsync(Guid id, CancellationToken ct = default)
    {
        var a = await _accounts.GetByIdAsync(id, ct);
        return a is null ? null : new AccountResponse(a.Id, a.Holder, a.Currency, a.AvailableBalance, a.CreatedAt);
    }

    public async Task<Guid> DepositAsync(Guid accountId, decimal amount, string? description, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be more than 0");

        await using var tx = await _uow.BeginTransactionAsync(ct);

        var acc = await _accounts.GetForUpdateAsync(accountId, ct) ?? throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");
        acc.AvailableBalance += amount;

        var tr = new LedgerTransaction { Id = Guid.NewGuid(), Type = "DEPOSIT", Description = description, Date = DateTime.UtcNow, AccountId = acc.Id, Amount = amount };
        await _txs.AddAsync(tr, ct);

        await _entries.AddAsync(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = tr.Id,
            AccountId = acc.Id,
            Debit = amount,
            Credit = 0,
            Currency = acc.Currency,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _outbox.AddAsync(new DomainEvent
        {
            Id = Guid.NewGuid(),
            Type = "DepositPerformed",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { accountId, amount, description, transactionId = tr.Id }),
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return tr.Id;
    }

    public async Task<Guid> WithdrawAsync(Guid accountId, decimal amount, string? description, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be > 0");

        await using var tx = await _uow.BeginTransactionAsync(ct);

        var acc = await _accounts.GetForUpdateAsync(accountId, ct) ?? throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");
        if (acc.AvailableBalance < amount) throw new InvalidOperationException("INSUFFICIENT_FUNDS");
        acc.AvailableBalance -= amount;

        var tr = new LedgerTransaction { Id = Guid.NewGuid(), Type = "WITHDRAWAL", Description = description, Date = DateTime.UtcNow, AccountId = acc.Id, Amount = amount };
        await _txs.AddAsync(tr, ct);

        await _entries.AddAsync(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = tr.Id,
            AccountId = acc.Id,
            Debit = 0,
            Credit = amount,
            Currency = acc.Currency,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _outbox.AddAsync(new DomainEvent
        {
            Id = Guid.NewGuid(),
            Type = "WithdrawalPerformed",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { accountId, amount, description, transactionId = tr.Id }),
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return tr.Id;
    }

    public async Task<Guid> TransferAsync(TransferRequest req, CancellationToken ct = default)
    {
        if (req.SourceAccountId == req.TargetAccountId)
            throw new InvalidOperationException("SAME_ACCOUNT");
        if (req.Amount <= 0)
            throw new ArgumentException("Amount can't be 0");

        await using var tx = await _uow.BeginTransactionAsync(ct);

        // Orden consistente por ID para evitar deadlocks
        var a = req.SourceAccountId;
        var b = req.TargetAccountId;
        if (a.CompareTo(b) > 0) (a, b) = (b, a);

        // Lock filas
        var first = await _accounts.GetForUpdateAsync(a, ct) ?? throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");
        var second = await _accounts.GetForUpdateAsync(b, ct) ?? throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");

        var source = first.Id == req.SourceAccountId ? first : second;
        var target = first.Id == req.TargetAccountId ? first : second;

        if (!string.Equals(source.Currency, req.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SOURCE_CURRENCY_MISMATCH");
        if (source.AvailableBalance < req.Amount)
            throw new InvalidOperationException("INSUFFICIENT_FUNDS");

        // --- FX + valuación base ---
        const string BASE = "DOP";
        decimal Rate(string from, string to) => _fx.GetRate(from, to);

        var sameCurrency = string.Equals(source.Currency, target.Currency, StringComparison.OrdinalIgnoreCase);
        decimal creditedToTarget;
        decimal? fxRate = null;
        string? fxPair = null;

        if (sameCurrency)
        {
            creditedToTarget = req.Amount;
        }
        else
        {
            fxRate = Rate(source.Currency, target.Currency);
            fxPair = $"{source.Currency}/{target.Currency}";
            creditedToTarget = Math.Round(req.Amount * fxRate.Value, 2, MidpointRounding.AwayFromZero);
        }

        // Update balances
        source.AvailableBalance -= req.Amount;
        target.AvailableBalance += creditedToTarget;

        // Transacciones (una por cuenta)
        var sourceTx = new LedgerTransaction
        {
            Id = Guid.NewGuid(),
            Type = sameCurrency ? "TRANSFER" : "TRANSFER_FX",
            Description = req.Description,
            Date = DateTime.UtcNow,
            Amount = req.Amount,
            AccountId = source.Id,
            FxPair = fxPair,
            FxRate = fxRate
        };
        var targetTx = new LedgerTransaction
        {
            Id = Guid.NewGuid(),
            Type = sameCurrency ? "TRANSFER" : "TRANSFER_FX",
            Description = req.Description,
            Date = DateTime.UtcNow,
            Amount = creditedToTarget,
            AccountId = target.Id,
            FxPair = fxPair,
            FxRate = fxRate
        };
        await _txs.AddRangeAsync([targetTx, sourceTx], ct);

        // Valuación a moneda base
        var baseSrc = Math.Round(req.Amount * Rate(source.Currency, BASE), 2, MidpointRounding.AwayFromZero);
        var baseDst = Math.Round(creditedToTarget * Rate(target.Currency, BASE), 2, MidpointRounding.AwayFromZero);

        var now = DateTime.UtcNow;

        // Asientos principales
        var srcEntry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = sourceTx.Id,
            AccountId = source.Id,
            Debit = 0,
            Credit = req.Amount,
            Currency = source.Currency,
            BaseCurrency = BASE,
            BaseDebit = 0,
            BaseCredit = baseSrc,
            FxRate = sameCurrency ? null : fxRate,
            CreatedAt = now
        };
        var dstEntry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = targetTx.Id,
            AccountId = target.Id,
            Debit = creditedToTarget,
            Credit = 0,
            Currency = target.Currency,
            BaseCurrency = BASE,
            BaseDebit = baseDst,
            BaseCredit = 0,
            FxRate = sameCurrency ? null : fxRate,
            CreatedAt = now
        };

        var entries = new List<LedgerEntry> { srcEntry, dstEntry };

        // Asiento de redondeo (si hiciera falta para que sum(BaseDebit)==sum(BaseCredit))
        var diff = Math.Round(baseDst - baseSrc, 2, MidpointRounding.AwayFromZero);
        if (diff != 0)
        {
            // Bloquear y ajustar la cuenta interna de rounding (DOP)
            var roundingId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var roundingAcc = await _accounts.GetForUpdateAsync(roundingId, ct)
                             ?? throw new InvalidOperationException("ROUNDING_ACCOUNT_MISSING");

            if (diff > 0)
            {
                // falta crédito en base → acreditamos rounding (Haber)
                roundingAcc.AvailableBalance -= diff; 
                entries.Add(new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TransactionId = targetTx.Id, // puedes asociarlo a cualquiera de los dos TX
                    AccountId = roundingAcc.Id,
                    Debit = 0,
                    Credit = Math.Abs(diff),
                    Currency = BASE,
                    BaseCurrency = BASE,
                    BaseDebit = 0,
                    BaseCredit = Math.Abs(diff),
                    FxRate = null,
                    CreatedAt = now
                });
            }
            else
            {
                // falta débito en base → debitamos rounding (Debe)
                var abs = Math.Abs(diff);
                roundingAcc.AvailableBalance += abs; // entra DOP a FX_ROUNDING
                entries.Add(new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TransactionId = targetTx.Id,
                    AccountId = roundingAcc.Id,
                    Debit = abs,
                    Credit = 0,
                    Currency = BASE,
                    BaseCurrency = BASE,
                    BaseDebit = abs,
                    BaseCredit = 0,
                    FxRate = null,
                    CreatedAt = now
                });
            }
        }

        await _entries.AddRangeAsync(entries, ct);

        // Outbox (incluye detalles FX y valuación base)
        await _outbox.AddAsync(new DomainEvent
        {
            Id = Guid.NewGuid(),
            Type = "TransferPerformed",
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                sourceAccountId = source.Id,
                targetAccountId = target.Id,
                sourceCurrency = source.Currency,
                targetCurrency = target.Currency,
                amount = req.Amount,
                creditedToTarget,
                fxPair,
                fxRate,
                baseCurrency = BASE,
                baseCreditSource = baseSrc,
                baseDebitTarget = baseDst,
                sourceTransactionId = sourceTx.Id,
                targetTransactionId = targetTx.Id,
                description = req.Description,
                roundingApplied = diff != 0 ? diff : 0
            }),
            CreatedAt = now
        }, ct);

        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return sourceTx.Id;
    }


}
