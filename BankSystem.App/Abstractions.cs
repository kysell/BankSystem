using System.Linq.Expressions;
using BankSystem.Domain.Models;

namespace BankSystem.App.Interfaces;

// Интерфейсы описывают операции. Сам код хранения находится в проекте Data.
public interface IStorage<T>
{
    IReadOnlyList<T> Get(Expression<Func<T, bool>>? filter = null);
    void Add(T item);
    void Update(T item);
    void Delete(T item);
    Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>>? filter = null, CancellationToken token = default);
    Task AddAsync(T item, CancellationToken token = default);
    Task UpdateAsync(T item, CancellationToken token = default);
    Task DeleteAsync(T item, CancellationToken token = default);
}

public interface IClientStorage : IStorage<Client>
{
    Client? GetById(Guid id);
    void AddAccount(Guid clientId, Account account);
    void UpdateAccount(Guid clientId, Account account);
    void DeleteAccount(Guid clientId, Guid accountId);
    Task<Client?> GetByIdAsync(Guid id, CancellationToken token = default);
    Task AddAccountAsync(Guid id, Account account, CancellationToken token = default);
    Task UpdateAccountAsync(Guid id, Account account, CancellationToken token = default);
    Task DeleteAccountAsync(Guid id, Guid accountId, CancellationToken token = default);
    Task WithdrawAsync(Guid id, Guid accountId, decimal amount, CancellationToken token = default);
}

public interface IEmployeeStorage : IStorage<Employee>
{
    Employee? GetById(Guid id);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken token = default);
}
