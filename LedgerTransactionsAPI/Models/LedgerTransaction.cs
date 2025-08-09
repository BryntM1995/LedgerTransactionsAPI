namespace LedgerTransactionsAPI.Models
{
    public class LedgerTransaction
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }       // Relación con Account
        public string? Type { get; set; }          // DEPOSITO, RETIRO, TRANSFERENCIA_ENTRANTE, TRANSFERENCIA_SALIENTE
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
