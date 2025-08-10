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
    }
}
