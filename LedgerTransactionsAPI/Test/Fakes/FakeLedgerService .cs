using System.Collections.Concurrent;
using LedgerTransactionsAPI.Dtos;
using LedgerTransactionsAPI.Models;
using LedgerTransactionsAPI.Services;

public sealed class FakeLedgerService : ILedgerService
{
    private readonly ConcurrentDictionary<Guid, Account> _accounts = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
    private readonly IFxRates _fx;

    public FakeLedgerService(IFxRates fx) => _fx = fx;

    // ----- helpers de seed -----
    public void SeedAccount(Guid id, string holder, string currency, decimal balance)
    {
        _accounts[id] = new Account
        {
            Id = id,
            Holder = holder,
            Currency = currency,
            AvailableBalance = balance,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };
        _locks.TryAdd(id, new SemaphoreSlim(1, 1));
    }

    private SemaphoreSlim GetLock(Guid id) => _locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));

    // ----- ILedgerService -----
    public Task<AccountResponse> CreateAccountAsync(CreateAccountRequest req, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        SeedAccount(id, req.Holder, req.Currency.ToUpperInvariant(), req.InitialBalance ?? 0m);
        var a = _accounts[id];
        return Task.FromResult(new AccountResponse(a.Id, a.Holder, a.Currency, a.AvailableBalance, a.CreatedAt));
    }

    public Task<AccountResponse?> GetAccountAsync(Guid id, CancellationToken ct = default)
    {
        if (!_accounts.TryGetValue(id, out var a)) return Task.FromResult<AccountResponse?>(null);
        return Task.FromResult<AccountResponse?>(new AccountResponse(a.Id, a.Holder, a.Currency, a.AvailableBalance, a.CreatedAt));
    }

    public async Task<Guid> DepositAsync(Guid accountId, decimal amount, string? description, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be > 0", nameof(amount));
        var l = GetLock(accountId);
        await l.WaitAsync(ct);
        try
        {
            if (!_accounts.TryGetValue(accountId, out var a)) throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");
            a.AvailableBalance += amount;
            return Guid.NewGuid();
        }
        finally { l.Release(); }
    }

    public async Task<Guid> WithdrawAsync(Guid accountId, decimal amount, string? description, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be > 0", nameof(amount));
        var l = GetLock(accountId);
        await l.WaitAsync(ct);
        try
        {
            if (!_accounts.TryGetValue(accountId, out var a)) throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");
            if (a.AvailableBalance < amount) throw new InvalidOperationException("INSUFFICIENT_FUNDS");
            a.AvailableBalance -= amount;
            return Guid.NewGuid();
        }
        finally { l.Release(); }
    }

    public async Task<Guid> TransferAsync(TransferRequest req, CancellationToken ct = default)
    {
        if (req.SourceAccountId == req.TargetAccountId) throw new InvalidOperationException("SAME_ACCOUNT");
        if (req.Amount <= 0) throw new ArgumentException("Amount can't be 0");

        // tomar locks en orden consistente
        var a = req.SourceAccountId; var b = req.TargetAccountId;
        if (a.CompareTo(b) > 0) (a, b) = (b, a);
        var la = GetLock(a); var lb = GetLock(b);

        await la.WaitAsync(ct);
        await lb.WaitAsync(ct);
        try
        {
            if (!_accounts.TryGetValue(req.SourceAccountId, out var src) ||
                !_accounts.TryGetValue(req.TargetAccountId, out var dst))
                throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");

            if (!string.Equals(src.Currency, req.Currency, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SOURCE_CURRENCY_MISMATCH");
            if (src.AvailableBalance < req.Amount)
                throw new InvalidOperationException("INSUFFICIENT_FUNDS");

            decimal credited;
            if (string.Equals(src.Currency, dst.Currency, StringComparison.OrdinalIgnoreCase))
            {
                credited = req.Amount;
            }
            else
            {
                var rate = _fx.GetRate(src.Currency, dst.Currency);
                credited = Math.Round(req.Amount * rate, 2, MidpointRounding.AwayFromZero);
            }

            src.AvailableBalance -= req.Amount;
            dst.AvailableBalance += credited;

            return Guid.NewGuid();
        }
        finally
        {
            lb.Release();
            la.Release();
        }
    }
}
