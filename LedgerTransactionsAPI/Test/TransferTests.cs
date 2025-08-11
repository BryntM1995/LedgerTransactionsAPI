using LedgerTransactionsAPI.Dtos;
using Xunit;

namespace LedgerTransactionsAPI.Test {
    public class TransferTests
    {
        [Fact]
        public async Task Transfer_Should_Throw_When_SameAccount()
        {
            var service = TestHelper.CreateFakeServiceWithSeed();
            var id = Guid.NewGuid();

            var req = new TransferRequest(id, id, 100m, "USD", "Test");
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.TransferAsync(req));
        }

        [Fact]
        public async Task Withdraw_Should_Throw_When_Insufficient_Funds()
        {
            var service = TestHelper.CreateFakeServiceWithSeed(dopABalance: 50m);
            var id = TestHelper.SeedDopA;

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.WithdrawAsync(id, 100m, "too much"));
            var acc = await service.GetAccountAsync(id);
            Assert.Equal(50m, acc!.AvailableBalance);
        }

        [Fact]
        public async Task Concurrency_100_Withdrawals_Should_Not_Go_Negative()
        {
            var service = TestHelper.CreateFakeServiceWithSeed(dopABalance: 1000m);
            var id = TestHelper.SeedDopA;

            var tasks = Enumerable.Range(0, 100)
                .Select(_ => Task.Run(async () =>
                {
                    try { await service.WithdrawAsync(id, 20m, "concurrent"); }
                    catch (InvalidOperationException ex) when (ex.Message == "INSUFFICIENT_FUNDS") { }
                }));

            await Task.WhenAll(tasks);

            var acc = await service.GetAccountAsync(id);
            Assert.True(acc!.AvailableBalance >= 0m);
        }

        [Fact]
        public async Task Transfer_FX_USD_to_DOP_Should_Apply_Rate()
        {
            var service = TestHelper.CreateFakeServiceWithSeed(dopABalance: 1000m, usdCBalance: 100m);
            var usd = TestHelper.SeedUsdC;
            var dop = TestHelper.SeedDopA;

            var req = new TransferRequest(usd, dop, 10m, "USD", "FX USD->DOP");
            await service.TransferAsync(req);

            var aUsd = await service.GetAccountAsync(usd);
            var aDop = await service.GetAccountAsync(dop);

            Assert.Equal(90m, aUsd!.AvailableBalance, 2);            // 100 - 10
            Assert.Equal(1000m + 571.40m, aDop!.AvailableBalance, 2); // 10 * 57.14
        }
    }
}
