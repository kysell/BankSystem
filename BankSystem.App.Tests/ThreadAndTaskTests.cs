using BankSystem.App.Services;
using BankSystem.Data.Storages;
using BankSystem.Domain.Models;
using NUnit.Framework;
using System.Threading.Channels;

namespace BankSystem.App.Tests;

public class ThreadAndTaskTests
{
    [Test]
    public void Account_TwoActualThreadsDepositWithoutLostUpdates()
    {
        var account = new Account();
        using var gate = new Barrier(2);
        void Deposit()
        {
            gate.SignalAndWait();
            for (var i = 0; i < 10; i++)
            {
                account.Deposit(100);
            }
        }

        var first = new Thread(Deposit);
        var second = new Thread(Deposit);
        first.Start();
        second.Start();
        first.Join();
        second.Join();
        Assert.That(account.Amount, Is.EqualTo(2000));
        Assert.Throws<InvalidOperationException>(() => account.Withdraw(2001));
        Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(0));
    }

    [Test]
    public async Task RateUpdater_CancelledToken_StopsBeforeWork()
    {
        var storage = new ClientStorage();
        var client = new Client();
        var account = new Account();
        account.Deposit(100);
        client.AddAccount(account);
        storage.Add(client);
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        var exception = Assert.CatchAsync(async () => await new RateUpdaterService(storage).ApplyMonthlyRateAsync(10, cancel.Token));
        Assert.That(exception, Is.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task Pipeline_ChannelInputRotatesByBytesAndRestoresAllAccounts()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pipeline-{Guid.NewGuid()}");
        try
        {
            var clients = Enumerable.Range(0, 10).Select(i => RegressionTests.Adult($"Клиент{i}")).ToArray();
            foreach (var client in clients)
            {
                var a = new Account();
                a.Deposit(10);
                client.AddAccount(a);
            }

            var channel = Channel.CreateBounded<Client>(2);
            var producer = Task.Run(async () =>
            {
                foreach (var client in clients)
                {
                    await channel.Writer.WriteAsync(client);
                }

                channel.Writer.Complete();
            });
            var pipeline = new ClientFilePipeline();
            var paths = await pipeline.WriteAsync(channel.Reader.ReadAllAsync(), directory, 1200);
            await producer;
            Assert.That(paths.Count, Is.GreaterThan(1));
            Assert.That(paths.All(p => new FileInfo(p).Length <= 1200), Is.True);
            var restored = new List<Client>();
            foreach (var path in paths)
            {
                restored.AddRange(await pipeline.ReadAsync(path));
            }

            Assert.That(restored.Select(c => c.Id), Is.EqualTo(clients.Select(c => c.Id)));
            Assert.That(restored.All(c => c.Accounts.Single().Amount == 10), Is.True);
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await pipeline.WriteAsync(channel.Reader.ReadAllAsync(), directory, 0));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

}
