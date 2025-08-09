namespace LedgerTransactionsAPI.Models
{
    public class DomainEvent
    {
        public Guid Id { get; set; }
        public string? Type { get; set; }          // Ej. TransferenciaRealizada
        public string? Payload { get; set; }       // JSON serializado
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Published { get; set; } = false;
        public DateTime? PublishedAt { get; set; }
    }
}
