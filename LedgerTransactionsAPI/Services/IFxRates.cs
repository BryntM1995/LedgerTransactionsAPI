// Services/IFxRates.cs
namespace LedgerTransactionsAPI.Services
{
    public interface IFxRates
    {
        decimal GetRate(string fromCurrency, string toCurrency);
    }
}
