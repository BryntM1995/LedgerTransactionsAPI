// Utils/Cursor.cs
using System.Text;

namespace LedgerTransactionsAPI.Utils;
public static class Cursor
{
    // cursor = base64("ticks:guid")
    public static string Encode(DateTime dt, Guid id)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{dt.Ticks}:{id}"));

    public static bool TryDecode(string? cursor, out DateTime dt, out Guid id)
    {
        dt = default; id = default;
        if (string.IsNullOrWhiteSpace(cursor)) return false;
        try
        {
            var s = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = s.Split(':', 2);
            if (parts.Length != 2) return false;
            dt = new DateTime(long.Parse(parts[0]), DateTimeKind.Utc);
            id = Guid.Parse(parts[1]);
            return true;
        }
        catch { return false; }
    }
}
