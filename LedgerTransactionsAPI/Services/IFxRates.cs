// Services/IFxRates.cs
namespace LedgerTransactionsAPI.Services
{
    public interface IFxRates
    {
        /// <summary>
        /// Devuelve la tasa de conversión from->to (ej. USD->DOP = 57.14).
        /// Si from==to, retorna 1m.
        /// Debe lanzar si la tasa no existe y no puede derivarse.
        /// </summary>
        decimal GetRate(string fromCurrency, string toCurrency);
    }
}
