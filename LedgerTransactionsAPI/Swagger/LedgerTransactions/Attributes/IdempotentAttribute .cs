namespace LedgerTransactionsAPI.Swagger.LedgerTransactions.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IdempotentAttribute : Attribute { }
}
