// Repositories/Ef/LedgerReadRepository.cs
using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Dtos;
using LedgerTransactionsAPI.Repositories.Interfaces;
using LedgerTransactionsAPI.Utils;
using Microsoft.EntityFrameworkCore;

namespace LedgerTransactionsAPI.Repositories.Ef;

public class LedgerReadRepository : ILedgerReadRepository
{
    private readonly LedgerDbContext _db;
    public LedgerReadRepository(LedgerDbContext db) => _db = db;

    private sealed class Row
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public Guid AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Currency { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public async Task<PagedResult<LedgerEntryItem>> ListAsync(
        Guid? accountId, int limit, string? cursor, PageDirection direction, CancellationToken ct)
    {
        DateTime? cDate = null; Guid? cId = null;
        if (Cursor.TryDecode(cursor, out var dt, out var gid)) { cDate = dt; cId = gid; }

        IQueryable<Row> baseQ = _db.LedgerEntries.AsNoTracking()
            .Select(x => new Row
            {
                Id = x.Id,
                TransactionId = x.TransactionId,
                AccountId = x.AccountId,
                Debit = x.Debit,
                Credit = x.Credit,
                Currency = x.Currency,
                CreatedAt = x.CreatedAt
            });

        if (accountId.HasValue)
            baseQ = baseQ.Where(x => x.AccountId == accountId.Value);

        IQueryable<Row> q;
        if (direction == PageDirection.Next)
        {
            q = baseQ.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);
            if (cDate.HasValue && cId.HasValue)
                q = q.Where(x => x.CreatedAt < cDate.Value || (x.CreatedAt == cDate.Value && x.Id.CompareTo(cId.Value) < 0));
        }
        else
        {
            q = baseQ.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id);
            if (cDate.HasValue && cId.HasValue)
                q = q.Where(x => x.CreatedAt > cDate.Value || (x.CreatedAt == cDate.Value && x.Id.CompareTo(cId.Value) > 0));
        }

        var rows = await q.Take(limit + 1).ToListAsync(ct);
        var page = rows.Take(limit).ToList();

        if (direction == PageDirection.Prev)
            page.Reverse(); // siempre devolvemos en DESC al cliente

        var items = page.Select(x => new LedgerEntryItem(
            x.Id, x.TransactionId, x.AccountId, x.Debit, x.Credit, x.Currency, x.CreatedAt
        )).ToList();

        string? next = null, prev = null;
        if (items.Count > 0)
        {
            var first = items.First();
            var last = items.Last();
            prev = Cursor.Encode(first.CreatedAt, first.Id); // ir hacia más “nuevo”
            next = Cursor.Encode(last.CreatedAt, last.Id);  // ir hacia más “viejo”
        }

        if (rows.Count <= limit)
        {
            if (direction == PageDirection.Next) next = null;
            else prev = null;
        }

        return new PagedResult<LedgerEntryItem>(items, next, prev);
    }
}
