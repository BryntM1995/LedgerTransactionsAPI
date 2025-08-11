namespace LedgerTransactionsAPI.Models
{
    public class LedgerEntry
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }   // Relación con LedgerTransaction
        public Guid AccountId { get; set; }       // Cuenta afectada
        public decimal Debit { get; set; }        // Monto en debe
        public decimal Credit { get; set; }       // Monto en haber
        public string? Currency { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔹 Valuación en moneda base (ej. DOP)
        public string? BaseCurrency { get; set; }        // "DOP"
        public decimal? BaseDebit { get; set; }          // valuación del Debit
        public decimal? BaseCredit { get; set; }         // valuación del Credit

        // 🔹 Tasa FX usada (opcional, útil para auditoría en la fila)
        public decimal? FxRate { get; set; }             // rate origen->destino cuando aplique
    }
}
