using System.Linq.Expressions;
using BankSystem.App.Interfaces;
using BankSystem.App.Services;
using BankSystem.Data.Storages;
using BankSystem.Domain.Models;
using NUnit.Framework;

namespace BankSystem.App.Tests;

public class MonthlyScheduleTests
{
    [Test]
    public async Task BackgroundLoop_AccruesOnNextCalendarMonthAndStopsOnCancellation()
    {
        var memory = new ClientStorage();
        var client = RegressionTests.Adult();
        var account = new Account();
        account.Deposit(100);
        client.AddAccount(account);
        memory.Add(client);
        var storage = new NotifyingStorage(memory);
        var clock = new ManualClock(new DateTimeOffset(2026, 1, 31, 23, 59, 59, TimeSpan.Zero));
        using var cts = new CancellationTokenSource();
        var loop = new RateUpdaterService(storage).RunAsync(5, clock, cts.Token);
        Assert.That(account.Amount, Is.EqualTo(100));
        clock.Advance(TimeSpan.FromSeconds(1));
        try
        {
            await storage.Applied.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(account.Amount, Is.EqualTo(105));
        }
        finally
        {
            cts.Cancel();
        }

        Assert.That(Assert.CatchAsync(async () => await loop), Is.InstanceOf<OperationCanceledException>());
    }

    private sealed class NotifyingStorage(ClientStorage inner) : IClientStorage
    {
        public Task<IReadOnlyList<Client>> GetAsync(Expression<Func<Client, bool>>? filter = null, CancellationToken token = default) => inner.GetAsync(filter, token);
        public Task<Client?> GetByIdAsync(Guid id, CancellationToken token = default) => inner.GetByIdAsync(id, token);
        public Task AddAsync(Client client, CancellationToken token = default) => inner.AddAsync(client, token);
        public Task UpdateAsync(Client client, CancellationToken token = default) => inner.UpdateAsync(client, token);
        public Task DeleteAsync(Client client, CancellationToken token = default) => inner.DeleteAsync(client, token);
        public Task AddAccountAsync(Guid id, Account account, CancellationToken token = default) => inner.AddAccountAsync(id, account, token);
        public async Task UpdateAccountAsync(Guid id, Account account, CancellationToken token = default)
        {
            await inner.UpdateAccountAsync(id, account, token);
            Applied.TrySetResult();
        }

        public Task DeleteAccountAsync(Guid id, Guid accountId, CancellationToken token = default) => inner.DeleteAccountAsync(id, accountId, token);
        public Task WithdrawAsync(Guid id, Guid accountId, decimal amount, CancellationToken token = default) => inner.WithdrawAsync(id, accountId, amount, token);
        public TaskCompletionSource Applied { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReadOnlyList<Client> Get(Expression<Func<Client, bool>>? filter = null) => inner.Get(filter);
        public void Add(Client client) => inner.Add(client);
        public void Update(Client client) => inner.Update(client);
        public void Delete(Client client) => inner.Delete(client);
        public Client? GetById(Guid id) => inner.GetById(id);
        public void AddAccount(Guid id, Account account) => inner.AddAccount(id, account);
        public void UpdateAccount(Guid id, Account account)
        {
            inner.UpdateAccount(id, account);
            Applied.TrySetResult();
        }

        public void DeleteAccount(Guid id, Guid accountId) => inner.DeleteAccount(id, accountId);
    }

    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        private readonly List<ManualTimer> _timers = [];
        private readonly object _sync = new();
        public override DateTimeOffset GetUtcNow()
        {
            lock (_sync)
            {
                return _now;
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            lock (_sync)
            {
                _timers.Add(timer);
            }

            timer.Change(dueTime, period);
            return timer;
        }

        public void Advance(TimeSpan delta)
        {
            ManualTimer[] ready;
            lock (_sync)
            {
                _now += delta;
                ready = _timers.Where(t => t.Due <= _now).ToArray();
                foreach (var timer in ready)
                {
                    timer.Due = DateTimeOffset.MaxValue;
                }
            }

            foreach (var timer in ready)
            {
                timer.Fire();
            }
        }

        private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
        {
            public DateTimeOffset Due { get; set; } = DateTimeOffset.MaxValue;

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (clock._sync)
                {
                    Due = dueTime == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : clock._now + dueTime;
                }

                return true;
            }

            public void Fire() => callback(state);
            public void Dispose()
            {
                lock (clock._sync)
                {
                    clock._timers.Remove(this);
                }
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
