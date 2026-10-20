using System.Linq.Expressions;
using BankSystem.App.Interfaces;
using BankSystem.Domain.Models;

namespace BankSystem.Data.Storages;

public sealed class ClientStorage : IClientStorage
{
    private readonly Dictionary<Guid, Client> _items = new Dictionary<Guid, Client>();
    private readonly object _sync = new object ();
    public IReadOnlyList<Client> Get(Expression<Func<Client, bool>>? filter = null)
    {
        lock (_sync)
        {
            if (filter == null)
            {
                return _items.Values.ToArray();
            }

            // В памяти выражение превращается в обычную функцию. EF выполняет его иначе — как SQL.
            Func<Client, bool> matchesFilter = filter.Compile();
            return _items.Values.Where(matchesFilter).ToArray();
        }
    }

    public Client? GetById(Guid id)
    {
        lock (_sync)
        {
            _items.TryGetValue(id, out Client? client);
            return client;
        }
    }

    public void Add(Client client)
    {
        lock (_sync)
        {
            if (_items.ContainsKey(client.Id))
            {
                throw new InvalidOperationException("Client already exists.");
            }

            _items.Add(client.Id, client);
        }
    }

    public void Update(Client client)
    {
        lock (_sync)
        {
            RequireClient(client.Id);
            _items[client.Id] = client;
        }
    }

    public void Delete(Client client)
    {
        lock (_sync)
        {
            if (!_items.Remove(client.Id))
            {
                throw new KeyNotFoundException("Client not found.");
            }
        }
    }

    public void AddAccount(Guid clientId, Account account)
    {
        lock (_sync)
        {
            Client client = RequireClient(clientId);
            client.AddAccount(account);
        }
    }

    public void UpdateAccount(Guid clientId, Account account)
    {
        lock (_sync)
        {
            Account savedAccount = FindAccount(clientId, account.Id);
            savedAccount.Currency = account.Currency;
            savedAccount.SetAmount(account.Amount);
        }
    }

    public void DeleteAccount(Guid clientId, Guid accountId)
    {
        lock (_sync)
        {
            Client client = RequireClient(clientId);
            if (!client.RemoveAccount(accountId))
            {
                throw new KeyNotFoundException("Account not found.");
            }
        }
    }

    // Для памяти нет ожидания ввода-вывода. Task.Run здесь не нужен.
    public Task<IReadOnlyList<Client>> GetAsync(Expression<Func<Client, bool>>? filter = null, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(Get(filter));
    }

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(GetById(id));
    }

    public Task AddAsync(Client client, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Add(client);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Client client, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Update(client);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Client client, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Delete(client);
        return Task.CompletedTask;
    }

    public Task AddAccountAsync(Guid id, Account account, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        AddAccount(id, account);
        return Task.CompletedTask;
    }

    public Task UpdateAccountAsync(Guid id, Account account, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        UpdateAccount(id, account);
        return Task.CompletedTask;
    }

    public Task DeleteAccountAsync(Guid id, Guid accountId, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        DeleteAccount(id, accountId);
        return Task.CompletedTask;
    }

    public Task WithdrawAsync(Guid id, Guid accountId, decimal amount, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        lock (_sync)
        {
            Account account = FindAccount(id, accountId);
            account.Withdraw(amount);
        }

        return Task.CompletedTask;
    }

    private Client RequireClient(Guid id)
    {
        Client? client = GetById(id);
        if (client == null)
        {
            throw new KeyNotFoundException("Client not found.");
        }

        return client;
    }

    private Account FindAccount(Guid clientId, Guid accountId)
    {
        Client client = RequireClient(clientId);
        foreach (Account account in client.Accounts)
        {
            if (account.Id == accountId)
            {
                return account;
            }
        }

        throw new KeyNotFoundException("Account does not belong to this client.");
    }
}

public sealed class EmployeeStorage : IEmployeeStorage
{
    private readonly Dictionary<Guid, Employee> _items = new Dictionary<Guid, Employee>();
    private readonly object _sync = new object ();
    public IReadOnlyList<Employee> Get(Expression<Func<Employee, bool>>? filter = null)
    {
        lock (_sync)
        {
            if (filter == null)
            {
                return _items.Values.ToArray();
            }

            Func<Employee, bool> matchesFilter = filter.Compile();
            return _items.Values.Where(matchesFilter).ToArray();
        }
    }

    public Employee? GetById(Guid id)
    {
        lock (_sync)
        {
            _items.TryGetValue(id, out Employee? employee);
            return employee;
        }
    }

    public void Add(Employee employee)
    {
        lock (_sync)
        {
            if (_items.ContainsKey(employee.Id))
            {
                throw new InvalidOperationException("Employee already exists.");
            }

            _items.Add(employee.Id, employee);
        }
    }

    public void Update(Employee employee)
    {
        lock (_sync)
        {
            if (!_items.ContainsKey(employee.Id))
            {
                throw new KeyNotFoundException("Employee not found.");
            }

            _items[employee.Id] = employee;
        }
    }

    public void Delete(Employee employee)
    {
        lock (_sync)
        {
            if (!_items.Remove(employee.Id))
            {
                throw new KeyNotFoundException("Employee not found.");
            }
        }
    }

    public Task<IReadOnlyList<Employee>> GetAsync(Expression<Func<Employee, bool>>? filter = null, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(Get(filter));
    }

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(GetById(id));
    }

    public Task AddAsync(Employee employee, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Add(employee);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Employee employee, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Update(employee);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Employee employee, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Delete(employee);
        return Task.CompletedTask;
    }
}
