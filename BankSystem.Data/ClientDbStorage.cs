using System.Linq.Expressions;
using BankSystem.App.Interfaces;
using BankSystem.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BankSystem.Data.Storages;

public sealed record ClientPage(IReadOnlyList<Client> Items, int TotalCount, int Page, int Size);
public sealed class ClientDbStorage : IClientStorage, IDisposable
{
    private readonly BankSystemDbContext _context;
    public ClientDbStorage(BankSystemDbContext context)
    {
        _context = context;
    }

    // Один DbContext нельзя использовать одновременно из нескольких задач.
    private readonly SemaphoreSlim _withdrawGate = new(1, 1);
    private IQueryable<Client> Query => _context.Clients.Include(client => client.Accounts).ThenInclude(account => account.Currency);

    private IQueryable<Client> CreateQuery(Expression<Func<Client, bool>>? filter)
    {
        IQueryable<Client> query = Query;
        if (filter != null)
        {
            query = query.Where(filter);
        }

        return query;
    }

    public IReadOnlyList<Client> Get(Expression<Func<Client, bool>>? filter = null)
    {
        IQueryable<Client> query = CreateQuery(filter).AsNoTracking();
        return query.ToList();
    }

    public async Task<IReadOnlyList<Client>> GetAsync(Expression<Func<Client, bool>>? filter = null, CancellationToken token = default)
    {
        IQueryable<Client> query = CreateQuery(filter).AsNoTracking();
        return await query.ToListAsync(token);
    }

    public Client? GetById(Guid id)
    {
        return Query.SingleOrDefault(client => client.Id == id);
    }

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        return Query.SingleOrDefaultAsync(client => client.Id == id, token);
    }
    public void Add(Client item)
    {
        EnsureDefault(item);
        foreach (var account in item.Accounts)
        {
            NormalizeCurrency(account);
        }

        _context.Clients.Add(item);
        _context.SaveChanges();
    }

    public async Task AddAsync(Client item, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        EnsureDefault(item);
        foreach (var account in item.Accounts)
        {
            await NormalizeCurrencyAsync(account, token);
        }

        _context.Clients.Add(item);
        await _context.SaveChangesAsync(token);
    }

    public void Update(Client item)
    {
        Client savedClient = Require(item.Id);
        Copy(item, savedClient);
        _context.SaveChanges();
    }

    public async Task UpdateAsync(Client item, CancellationToken token = default)
    {
        Client savedClient = await RequireAsync(item.Id, token);
        Copy(item, savedClient);
        await _context.SaveChangesAsync(token);
    }

    public void Delete(Client item)
    {
        Client savedClient = Require(item.Id);
        _context.Clients.Remove(savedClient);
        _context.SaveChanges();
    }

    public async Task DeleteAsync(Client item, CancellationToken token = default)
    {
        Client savedClient = await RequireAsync(item.Id, token);
        _context.Clients.Remove(savedClient);
        await _context.SaveChangesAsync(token);
    }

    public void AddAccount(Guid clientId, Account account)
    {
        var client = Require(clientId);
        NormalizeCurrency(account);
        client.AddAccount(account);
        _context.Accounts.Add(account);
        _context.SaveChanges();
    }

    public async Task AddAccountAsync(Guid id, Account account, CancellationToken token = default)
    {
        var client = await RequireAsync(id, token);
        await NormalizeCurrencyAsync(account, token);
        client.AddAccount(account);
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(token);
    }

    public void UpdateAccount(Guid id, Account account)
    {
        var target = FindAccount(Require(id), account.Id);
        NormalizeCurrency(account);
        CopyAccount(account, target);
        _context.SaveChanges();
    }

    public async Task UpdateAccountAsync(Guid id, Account account, CancellationToken token = default)
    {
        Client client = await RequireAsync(id, token);
        Account target = FindAccount(client, account.Id);
        await NormalizeCurrencyAsync(account, token);
        CopyAccount(account, target);
        await _context.SaveChangesAsync(token);
    }

    public void DeleteAccount(Guid id, Guid accountId)
    {
        _context.Accounts.Remove(FindAccount(Require(id), accountId));
        _context.SaveChanges();
    }

    public async Task DeleteAccountAsync(Guid id, Guid accountId, CancellationToken token = default)
    {
        Client client = await RequireAsync(id, token);
        Account account = FindAccount(client, accountId);
        _context.Accounts.Remove(account);
        await _context.SaveChangesAsync(token);
    }

    public async Task WithdrawAsync(Guid id, Guid accountId, decimal amount, CancellationToken token = default)
    {
        await _withdrawGate.WaitAsync(token);
        try
        {
            Client client = await RequireAsync(id, token);
            Account account = FindAccount(client, accountId);
            account.Withdraw(amount);
            await _context.SaveChangesAsync(token);
        }
        finally
        {
            _withdrawGate.Release();
        }
    }

    public ClientPage Search(string? name, int page, int size)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        if (size < 1 || size > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        var query = Query.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(x => (x.FirstName + " " + x.LastName).Contains(name));
        }

        int totalCount = query.Count();
        int skip = checked((page - 1) * size);
        List<Client> clients = query.OrderBy(client => client.LastName).ThenBy(client => client.Id).Skip(skip).Take(size).ToList();
        return new ClientPage(clients, totalCount, page, size);
    }

    private void NormalizeCurrency(Account account)
    {
        Currency? currency = _context.Currencies.Local.SingleOrDefault(item => item.Code == account.Currency.Code);
        if (currency == null)
        {
            currency = _context.Currencies.Find(account.Currency.Code);
        }

        if (currency is null)
        {
            _context.Currencies.Add(account.Currency);
        }
        else
        {
            account.Currency = currency;
        }
    }

    private async Task NormalizeCurrencyAsync(Account account, CancellationToken token)
    {
        Currency? currency = _context.Currencies.Local.SingleOrDefault(item => item.Code == account.Currency.Code);
        if (currency == null)
        {
            currency = await _context.Currencies.FindAsync(new object[] { account.Currency.Code }, token);
        }

        if (currency is null)
        {
            _context.Currencies.Add(account.Currency);
        }
        else
        {
            account.Currency = currency;
        }
    }

    private static void EnsureDefault(Client client)
    {
        if (client.Accounts.Count == 0)
        {
            client.AddAccount(new Account());
        }
    }

    private static void Copy(Client source, Client target)
    {
        target.FirstName = source.FirstName;
        target.LastName = source.LastName;
        target.BirthDate = source.BirthDate;
        target.PhoneNumber = source.PhoneNumber;
        target.PassportNumber = source.PassportNumber;
        target.Bonus = source.Bonus;
    }

    private static void CopyAccount(Account source, Account target)
    {
        target.Currency = source.Currency;
        target.SetAmount(source.Amount);
    }

    private static Account FindAccount(Client client, Guid id)
    {
        foreach (Account account in client.Accounts)
        {
            if (account.Id == id)
            {
                return account;
            }
        }

        throw new KeyNotFoundException("Account does not belong to this client.");
    }

    private Client Require(Guid id)
    {
        Client? client = GetById(id);
        if (client == null)
        {
            throw new KeyNotFoundException("Client not found.");
        }

        return client;
    }

    private async Task<Client> RequireAsync(Guid id, CancellationToken token)
    {
        Client? client = await GetByIdAsync(id, token);
        if (client == null)
        {
            throw new KeyNotFoundException("Client not found.");
        }

        return client;
    }

    public void Dispose() => _withdrawGate.Dispose();
}
