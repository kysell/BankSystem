using BankSystem.App.Services;
using BankSystem.Data.Storages;
using NUnit.Framework;

namespace BankSystem.Data.Tests;

public class RateUpdaterServiceTests
{
    [TestCase("2026-01-31T23:59:59Z", 1)]
    [TestCase("2028-02-29T12:00:00Z", 43200)]
    [TestCase("2026-12-31T12:00:00Z", 43200)]
    public void MonthlyDelay_UsesCalendarMonth(string now, int seconds)
    {
        TimeSpan delay = RateUpdaterService.TimeUntilNextMonth(DateTimeOffset.Parse(now));
        Assert.That(delay, Is.EqualTo(TimeSpan.FromSeconds(seconds)));
    }

    [Test]
    public async Task BackgroundLoop_CanBeCancelledWhileWaiting()
    {
        var storage = new ClientStorage();
        using var cancellation = new CancellationTokenSource();
        var service = new RateUpdaterService(storage);
        Task loop = service.RunAsync(5, cancellation.Token);
        Assert.That(loop.IsCompleted, Is.False);
        cancellation.Cancel();
        try
        {
            await loop;
            Assert.Fail("Ожидалась отмена фонового цикла.");
        }
        catch (OperationCanceledException)
        {
            Assert.That(loop.IsCanceled, Is.True);
        }
    }
}
