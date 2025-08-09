namespace LedgerTransactionsAPI.Models
{
    public class IdempotencyKey
    {
        public string? Key { get; set; }           // Header Idempotency-Key
        public string? RequestHash { get; set; }   // Hash de la request
        public int ResponseCode { get; set; }     // Código HTTP que se devolvió
        public string? ResponseBody { get; set; }  // JSON del resultado
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
