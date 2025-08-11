using LedgerTransactionsAPI.Services;

public static class TestHelper
{
    public static readonly Guid SeedDopA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid SeedDopB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid SeedUsdC = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public static FakeLedgerService CreateFakeServiceWithSeed(
        decimal dopABalance = 10_000m, decimal dopBBalance = 2_500m, decimal usdCBalance = 100m)
    {
        var svc = new FakeLedgerService(new TestFxRates());
        svc.SeedAccount(SeedDopA, "A", "DOP", dopABalance);
        svc.SeedAccount(SeedDopB, "B", "DOP", dopBBalance);
        svc.SeedAccount(SeedUsdC, "C", "USD", usdCBalance);
        return svc;
    }
}
