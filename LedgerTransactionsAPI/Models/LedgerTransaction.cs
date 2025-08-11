namespace LedgerTransactionsAPI.Models
{
    public class LedgerTransaction
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }       
        public string? Type { get; set; }        
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string? FxPair { get; set; }   // "USD/DOP"
        public decimal? FxRate { get; set; }  // 57.14 o 0.0175, etc.
    }
}
