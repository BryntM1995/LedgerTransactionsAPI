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

    public LedgerService(
        IUnitOfWork uow,
        IAccountRepository accounts,
        ITransactionRepository txs,
        ILedgerEntryRepository entries,
        IOutboxRepository outbox)
    {
        _uow = uow;
        _accounts = accounts;
        _txs = txs;
        _entries = entries;
        _outbox = outbox;
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
        if (amount <= 0) throw new ArgumentException("Amount must be > 0");

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

        // Lock 1
        var first = await _accounts.GetForUpdateAsync(a, ct)
                     ?? throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");
        // Lock 2
        var second = await _accounts.GetForUpdateAsync(b, ct)
                     ?? throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");

        // Reasignamos cuál es source/target según los IDs originales
        var source = first.Id == req.SourceAccountId ? first : second;
        var target = first.Id == req.TargetAccountId ? first : second;

        if (!string.Equals(source.Currency, req.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SOURCE_CURRENCY_MISMATCH");
        if (source.AvailableBalance < req.Amount)
            throw new InvalidOperationException("INSUFFICIENT_FUNDS");

        source.AvailableBalance -= req.Amount;
        target.AvailableBalance += req.Amount;

        var soruceTransaction = new LedgerTransaction
        {
            Id = Guid.NewGuid(),
            Type = "TRANSFER",
            Description = req.Description,
            Date = DateTime.UtcNow,
            Amount = req.Amount,
            AccountId = source.Id,
        };

        var targetTransaction = new LedgerTransaction
        {
            Id = Guid.NewGuid(),
            Type = "TRANSFER",
            Description = req.Description,
            Date = DateTime.UtcNow,
            Amount = req.Amount,
            AccountId = target.Id,
        };

        await _txs.AddRangeAsync([targetTransaction , soruceTransaction], ct);

        await _entries.AddRangeAsync(
        [
        new LedgerEntry { Id = Guid.NewGuid(), TransactionId = soruceTransaction.Id, AccountId = source.Id, Debit = 0, Credit = req.Amount, Currency = source.Currency, CreatedAt = DateTime.UtcNow },
        new LedgerEntry { Id = Guid.NewGuid(), TransactionId = targetTransaction.Id, AccountId = target.Id, Debit = req.Amount, Credit = 0, Currency = target.Currency, CreatedAt = DateTime.UtcNow }
    ], ct);

        await _outbox.AddAsync(new DomainEvent
        {
            Id = Guid.NewGuid(),
            Type = "TransferPerformed",
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                sourceAccountId = source.Id,
                targetAccountId = target.Id,
                amount = req.Amount,
                currency = source.Currency,
                transactionId = soruceTransaction.Id,
                description = req.Description
            }),
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return soruceTransaction.Id;
    }

}
