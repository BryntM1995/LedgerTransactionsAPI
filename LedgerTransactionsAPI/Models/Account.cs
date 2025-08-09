using System;

namespace LedgerTransactionsAPI.Models
{
    public class Account
    {
        public Guid Id { get; set; }               
        public string? Holder { get; set; }        
        public string? Currency { get; set; }      
        public decimal AvailableBalance { get; set; }
        public int Version { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
