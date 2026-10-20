using System.Linq.Expressions;
using BankSystem.App.Dto;
using BankSystem.App.Interfaces;
using BankSystem.App.Validators;
using BankSystem.Domain.Models;
using FluentValidation;

namespace BankSystem.App.Services;

public sealed class ClientService
{
    private readonly IClientStorage _storage;
    private readonly IValidator<ClientRequest> _validator;
    public ClientService(IClientStorage storage, IValidator<ClientRequest>? validator = null)
    {
        _storage = storage;
        _validator = validator ?? new ClientRequestValidator();
    }

    public async Task<ClientResponse?> GetResponseAsync(Guid id, CancellationToken token = default)
    {
        Client? client = await GetAsync(id, token);
        if (client == null)
        {
            return null;
        }

        return client.ToResponse();
    }

    public async Task<IReadOnlyList<ClientResponse>> SearchResponsesAsync(ClientFilter filter, CancellationToken token = default)
    {
        IReadOnlyList<Client> clients = await SearchAsync(filter, token);
        var responses = new List<ClientResponse>();
        foreach (Client client in clients)
        {
            responses.Add(client.ToResponse());
        }

        return responses;
    }

    public async Task<ClientResponse> CreateAsync(ClientRequest request, CancellationToken token = default)
    {
        await _validator.ValidateAndThrowAsync(request, token);
        Client client = request.ToModel();
        await AddClientAsync(client, token);
        return client.ToResponse();
    }

    public async Task UpdateAsync(Guid id, ClientRequest request, CancellationToken token = default)
    {
        await _validator.ValidateAndThrowAsync(request, token);
        Client? previousClient = await GetAsync(id, token);
        if (previousClient == null)
        {
            throw new KeyNotFoundException("Client not found.");
        }

        // Сначала проверяем новые данные; существующий объект пока не меняем.
        Client updatedClient = request.ToModel(id);
        updatedClient.Bonus = previousClient.Bonus;
        foreach (Account account in previousClient.Accounts)
        {
            updatedClient.AddAccount(account);
        }

        await UpdateAsync(updatedClient, token);
    }

    public void AddClient(Client client)
    {
        PrepareClient(client);
        _storage.Add(client);
    }

    public async Task AddClientAsync(Client client, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        PrepareClient(client);
        await _storage.AddAsync(client, token);
    }

    public Task<Client?> GetAsync(Guid id, CancellationToken token = default)
    {
        return _storage.GetByIdAsync(id, token);
    }

    public void UpdateClient(Client client)
    {
        ValidateClient(client);
        _storage.Update(client);
    }

    public Task UpdateAsync(Client client, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ValidateClient(client);
        return _storage.UpdateAsync(client, token);
    }

    public async Task DeleteAsync(Guid id, CancellationToken token = default)
    {
        Client? client = await _storage.GetByIdAsync(id, token);
        if (client == null)
        {
            throw new KeyNotFoundException("Client not found.");
        }

        await _storage.DeleteAsync(client, token);
    }

    public void AddAccount(Guid clientId, Account account)
    {
        ValidateAccount(account);
        _storage.AddAccount(clientId, account);
    }

    public Task AddAccountAsync(Guid id, Account account, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ValidateAccount(account);
        return _storage.AddAccountAsync(id, account, token);
    }

    public void UpdateAccount(Guid clientId, Account account)
    {
        ValidateAccount(account);
        _storage.UpdateAccount(clientId, account);
    }

    public Task UpdateAccountAsync(Guid id, Account account, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ValidateAccount(account);
        return _storage.UpdateAccountAsync(id, account, token);
    }

    public void DeleteAccount(Guid clientId, Guid accountId)
    {
        _storage.DeleteAccount(clientId, accountId);
    }

    public Task DeleteAccountAsync(Guid id, Guid accountId, CancellationToken token = default)
    {
        return _storage.DeleteAccountAsync(id, accountId, token);
    }

    public IReadOnlyList<Client> Search(ClientFilter filter)
    {
        return _storage.Get(CreateFilter(filter));
    }

    public Task<IReadOnlyList<Client>> SearchAsync(ClientFilter filter, CancellationToken token = default)
    {
        return _storage.GetAsync(CreateFilter(filter), token);
    }

    public Task WithdrawManyAsync(IEnumerable<(Guid ClientId, Guid AccountId, decimal Amount)> requests, CancellationToken token = default)
    {
        var tasks = new List<Task>();
        foreach (var request in requests)
        {
            Task withdrawal = _storage.WithdrawAsync(request.ClientId, request.AccountId, request.Amount, token);
            tasks.Add(withdrawal);
        }

        return Task.WhenAll(tasks);
    }

    private static Expression<Func<Client, bool>> CreateFilter(ClientFilter filter)
    {
        if (filter.BornFrom > filter.BornTo)
        {
            throw new ArgumentException("Birth date range is reversed.");
        }

        string? name = filter.FullName?.Trim().ToLowerInvariant();
        // Это выражение EF переводит в SQL. Внутри него нельзя вызывать произвольные методы C#.
        return client =>
            (name == null || name == "" || (client.FirstName + " " + client.LastName).ToLower().Contains(name)) &&
            (filter.PhoneNumber == null || client.PhoneNumber == filter.PhoneNumber) &&
            (filter.PassportNumber == null || client.PassportNumber == filter.PassportNumber) &&
            (!filter.BornFrom.HasValue || client.BirthDate >= filter.BornFrom.Value) &&
            (!filter.BornTo.HasValue || client.BirthDate <= filter.BornTo.Value);
    }

    private static void PrepareClient(Client client)
    {
        ValidateClient(client);
        if (client.Accounts.Count == 0)
        {
            client.AddAccount(new Account());
        }
    }

    private static void ValidateClient(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (client.Age < 18)
        {
            throw new UnderageClientException("Client must be at least 18 years old.");
        }

        PersonValidation.Validate(client);
        foreach (Account account in client.Accounts)
        {
            ValidateAccount(account);
        }
    }

    private static void ValidateAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (account.Id == Guid.Empty)
        {
            throw new ArgumentException("Account ID is required.");
        }

        if (account.Currency == null || string.IsNullOrWhiteSpace(account.Currency.Code) || account.Currency.Code.Length != 3)
        {
            throw new ArgumentException("Account requires a three-letter currency code.");
        }

        foreach (char letter in account.Currency.Code)
        {
            if (!char.IsAsciiLetter(letter))
            {
                throw new ArgumentException("Currency code must contain only letters.");
            }
        }

        if (account.Currency.ExchangeRate <= 0)
        {
            throw new ArgumentException("Exchange rate must be positive.");
        }
    }
}
