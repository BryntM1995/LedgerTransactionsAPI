// Services/FxRatesStub.cs
using LedgerTransactionsAPI.Services;

public class FxRatesStub : IFxRates
{
    private sealed class PairComparer : IEqualityComparer<(string from, string to)>
    {
        public bool Equals((string from, string to) x, (string from, string to) y) =>
            string.Equals(x.from, y.from, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.to, y.to, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string from, string to) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.from ?? string.Empty),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.to ?? string.Empty));
    }

    private static readonly Dictionary<(string from, string to), decimal> _rates =
        new Dictionary<(string from, string to), decimal>(new PairComparer())
        {
            { ("DOP","USD"), 0.0175m },
            { ("USD","DOP"), 57.14m }
        };

    public decimal GetRate(string fromCurrency, string toCurrency)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        if (_rates.TryGetValue((fromCurrency, toCurrency), out var direct))
            return direct;

        if (_rates.TryGetValue((toCurrency, fromCurrency), out var inverse) && inverse != 0)
            return Math.Round(1m / inverse, 6, MidpointRounding.AwayFromZero);

        throw new InvalidOperationException($"FX rate not available: {fromCurrency}->{toCurrency}");
    }
}
