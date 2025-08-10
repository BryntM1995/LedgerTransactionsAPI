using LedgerTransactionsAPI.Data;
using LedgerTransactionsAPI.Dtos;
using LedgerTransactionsAPI.Repositories.Interfaces;
using LedgerTransactionsAPI.Utils;
using Microsoft.EntityFrameworkCore;

namespace LedgerTransactionsAPI.Repositories.Ef;

public class TransactionReadRepository : ITransactionReadRepository
{
    private readonly LedgerDbContext _db;
    public TransactionReadRepository(LedgerDbContext db) => _db = db;

    // Proyección TIPADA (evita dynamic / anónimos en IQueryable)
    private sealed class TxRow
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "";
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Currency { get; set; } = "";
    }

    public async Task<PagedResult<TransactionItem>> ListByAccountAsync(
        Guid accountId, int limit, string? cursor, PageDirection direction, CancellationToken ct)
    {
        DateTime? cDate = null; Guid? cId = null;
        if (Cursor.TryDecode(cursor, out var dt, out var gid)) { cDate = dt; cId = gid; }

        IQueryable<TxRow> baseQ =
            from le in _db.LedgerEntries.AsNoTracking()
            join t in _db.Transactions.AsNoTracking() on le.TransactionId equals t.Id
            where le.AccountId == accountId
            select new TxRow
            {
                Id = t.Id,
                Type = t.Type,
                Description = t.Description,
                Date = t.Date,
                Debit = le.Debit,
                Credit = le.Credit,
                Currency = le.Currency
            };

        IQueryable<TxRow> q;

        if (direction == PageDirection.Next)
        {
            // Más nuevos -> más viejos
            q = baseQ.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id);

            if (cDate.HasValue && cId.HasValue)
            {
                // (Date, Id) < (cursorDate, cursorId)
                q = q.Where(x =>
                    x.Date < cDate.Value ||
                    (x.Date == cDate.Value && x.Id.CompareTo(cId.Value) < 0));
            }
        }
        else
        {
            // Para ir hacia "atrás" usamos orden ascendente y luego invertimos
            q = baseQ.OrderBy(x => x.Date).ThenBy(x => x.Id);

            if (cDate.HasValue && cId.HasValue)
            {
                // (Date, Id) > (cursorDate, cursorId)
                q = q.Where(x =>
                    x.Date > cDate.Value ||
                    (x.Date == cDate.Value && x.Id.CompareTo(cId.Value) > 0));
            }
        }

        var rows = await q.Take(limit + 1).ToListAsync(ct);
        var page = rows.Take(limit).ToList();

        if (direction == PageDirection.Prev)
            page.Reverse(); // devolvemos siempre en DESC al cliente

        var items = page.Select(x =>
        {
            var amount = x.Debit > 0 ? x.Debit : -x.Credit; // Debe(+), Haber(-)
            return new TransactionItem(x.Id, x.Type, x.Description, x.Date, amount, x.Currency);
        }).ToList();

        string? next = null, prev = null;
        if (items.Count > 0)
        {
            var first = items.First();
            var last = items.Last();
            prev = Cursor.Encode(first.Date, first.Id); // para ir hacia más nuevo
            next = Cursor.Encode(last.Date, last.Id);  // para ir hacia más viejo
        }

        // si no hay más en esa dirección, nulifica
        if (rows.Count <= limit)
        {
            if (direction == PageDirection.Next) next = null;
            else prev = null;
        }

        return new PagedResult<TransactionItem>(items, next, prev);
    }
}
