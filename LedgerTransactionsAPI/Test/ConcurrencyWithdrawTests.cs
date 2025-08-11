using System.Collections.Concurrent;
using Xunit;

namespace LedgerTransactionsAPI.Test
{
    public class ConcurrencyWithdrawTests
    {
        [Fact]
        public async Task OneHundredConcurrentWithdrawals_Should_NeverGoNegative()
        {
            // Saldo inicial 1000; cada retiro 20
            var service = TestHelper.CreateFakeServiceWithSeed(dopABalance: 1000m);
            var accountId = TestHelper.SeedDopA;
            const int attempts = 100;
            const decimal perWithdrawal = 20m;

            var successes = 0;
            var failures = 0;
            var tasks = new List<Task>(attempts);

            for (int i = 0; i < attempts; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await service.WithdrawAsync(accountId, perWithdrawal, "concurrent");
                        Interlocked.Increment(ref successes);
                    }
                    catch (InvalidOperationException ex) when (ex.Message == "INSUFFICIENT_FUNDS")
                    {
                        Interlocked.Increment(ref failures);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Nunca negativo
            var acc = await service.GetAccountAsync(accountId);
            Assert.NotNull(acc);
            Assert.True(acc!.AvailableBalance >= 0m);

            // A lo sumo 50 retiros (1000/20)
            Assert.InRange(successes, 0, 50);
            // Suma consistente (éxitos + fallos = 100)
            Assert.Equal(attempts, successes + failures);
            // Saldo final consistente: 1000 - successes*20
            Assert.Equal(1000m - successes * perWithdrawal, acc.AvailableBalance);
        }
    }

}
