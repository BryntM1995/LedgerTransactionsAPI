using LedgerTransactionsAPI.Services;

public sealed class TestFxRates : IFxRates
{
    public decimal GetRate(string fromCurrency, string toCurrency)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase)) return 1m;
        if (fromCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase) && toCurrency.Equals("DOP", StringComparison.OrdinalIgnoreCase)) return 57.14m;
        if (fromCurrency.Equals("DOP", StringComparison.OrdinalIgnoreCase) && toCurrency.Equals("USD", StringComparison.OrdinalIgnoreCase)) return 0.0175m;
        throw new InvalidOperationException($"FX rate not available: {fromCurrency}->{toCurrency}");
    }
}
