using BankSystem.App.Interfaces;
using BankSystem.Domain.Models;

namespace BankSystem.App.Services;

public sealed class RateUpdaterService
{
    private readonly IClientStorage _storage;
    public RateUpdaterService(IClientStorage storage)
    {
        _storage = storage;
    }

    public async Task ApplyMonthlyRateAsync(decimal ratePercent, CancellationToken token = default)
    {
        if (ratePercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ratePercent));
        }

        IReadOnlyList<Client> clients = await _storage.GetAsync(token: token);
        foreach (Client client in clients)
        {
            foreach (Account account in client.Accounts)
            {
                token.ThrowIfCancellationRequested();
                decimal interest = decimal.Round(account.Amount * ratePercent / 100m, 2);
                if (interest == 0)
                {
                    continue;
                }

                account.Deposit(interest);
                await _storage.UpdateAccountAsync(client.Id, account, token);
            }
        }
    }

    public async Task RunAsync(decimal ratePercent, CancellationToken token = default)
    {
        if (ratePercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ratePercent));
        }

        while (true)
        {
            token.ThrowIfCancellationRequested();
            TimeSpan delay = TimeUntilNextMonth(DateTimeOffset.UtcNow);
            await Task.Delay(delay, token);
            await ApplyMonthlyRateAsync(ratePercent, token);
        }
    }

    public static TimeSpan TimeUntilNextMonth(DateTimeOffset now)
    {
        // Работаем по UTC и ждём начало следующего календарного месяца.
        now = now.ToUniversalTime();
        var firstDay = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset nextMonth = firstDay.AddMonths(1);
        return nextMonth - now;
    }
}
