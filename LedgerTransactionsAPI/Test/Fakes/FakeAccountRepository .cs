// Tests/Fakes.cs
using System.Collections.Concurrent;
using LedgerTransactionsAPI.Models;
using LedgerTransactionsAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

public sealed class FakeAccountRepository : IAccountRepository
{
    private readonly ConcurrentDictionary<Guid, Account> _store = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    private SemaphoreSlim LockFor(Guid id) => _locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));

    public Task AddAsync(Account account, CancellationToken ct = default)
    {
        _store[account.Id] = Clone(account);
        return Task.CompletedTask;
    }

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var a) ? Clone(a) : null);

    // Simula SELECT ... FOR UPDATE
    public async Task<Account?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        var sem = LockFor(id);
        await sem.WaitAsync(ct);
        try
        {
            return _store.TryGetValue(id, out var a) ? a : null; // devolvemos la referencia real bloqueada
        }
        catch
        {
            sem.Release();
            throw;
        }
    }

    // Lock 2 filas en orden consistente para evitar deadlocks
    public async Task<(Account source, Account target)> GetPairForUpdateAsync(Guid sourceId, Guid targetId, CancellationToken ct = default)
    {
        var a = sourceId; var b = targetId;
        if (a.CompareTo(b) > 0) (a, b) = (b, a);

        var la = LockFor(a);
        var lb = LockFor(b);

        await la.WaitAsync(ct);
        await lb.WaitAsync(ct);

        try
        {
            var first = _store.TryGetValue(a, out var fa) ? fa : null;
            var second = _store.TryGetValue(b, out var fb) ? fb : null;
            if (first is null || second is null) throw new KeyNotFoundException("ACCOUNT_NOT_FOUND");

            // reasignamos para devolver en el orden pedido (sourceId/targetId)
            var source = (first.Id == sourceId) ? first : second;
            var target = (first.Id == targetId) ? first : second;
            return (source!, target!);
        }
        catch
        {
            lb.Release();
            la.Release();
            throw;
        }
    }

    public void Update(Account account)
    {
        // ya tenemos la referencia bloqueada; el cambio se refleja directo
        // no hacemos nada aquí.
    }

    public void Release(Guid id)
    {
        if (_locks.TryGetValue(id, out var sem))
            sem.Release();
    }

    public void ReleasePair(Guid a, Guid b)
    {
        // liberar en orden inverso a como tomamos
        if (a.CompareTo(b) > 0) (a, b) = (b, a);
        if (_locks.TryGetValue(b, out var sb)) sb.Release();
        if (_locks.TryGetValue(a, out var sa)) sa.Release();
    }

    private static Account Clone(Account a) => new Account
    {
        Id = a.Id,
        Holder = a.Holder,
        Currency = a.Currency,
        AvailableBalance = a.AvailableBalance,
        Version = a.Version,
        CreatedAt = a.CreatedAt
    };
}

public sealed class FakeTransactionRepository : ITransactionRepository
{
    public List<LedgerTransaction> Captured { get; } = new();
    public Task AddAsync(LedgerTransaction tx, CancellationToken ct = default)
    { Captured.Add(tx); return Task.CompletedTask; }

    public Task AddRangeAsync(IEnumerable<LedgerTransaction> txs, CancellationToken ct = default)
    { Captured.AddRange(txs); return Task.CompletedTask; }
}

public sealed class FakeLedgerEntryRepository : ILedgerEntryRepository
{
    public List<LedgerEntry> Captured { get; } = new();
    public Task AddAsync(LedgerEntry entry, CancellationToken ct = default)
    { Captured.Add(entry); return Task.CompletedTask; }

    public Task AddRangeAsync(IEnumerable<LedgerEntry> entries, CancellationToken ct = default)
    { Captured.AddRange(entries); return Task.CompletedTask; }
}

public sealed class FakeOutboxRepository : IOutboxRepository
{
    public List<DomainEvent> Events { get; } = new();
    public Task AddAsync(DomainEvent ev, CancellationToken ct = default)
    { Events.Add(ev); return Task.CompletedTask; }
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    // Transacción fake que conoce los locks a liberar
    private readonly FakeAccountRepository _accounts;

    public FakeUnitOfWork(FakeAccountRepository accounts) => _accounts = accounts;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

    public Task<IAsyncDisposable> BeginTransactionAsync(CancellationToken ct = default)
    {
        // devolvemos un disposable que al Dispose no hace nada; los locks los libera el service
        return Task.FromResult<IAsyncDisposable>(new NoopTx());
    }

    Task<IDbContextTransaction> IUnitOfWork.BeginTransactionAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private sealed class NoopTx : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
