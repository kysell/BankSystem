using System.Diagnostics;
using BankSystem.Domain.Models;
using Bogus;

namespace BankSystem.App.Services;

public sealed class TestDataGenerator
{
    public List<Client> CreateClients(int count = 1000)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var clients = new List<Client>();
        for (int number = 1; number <= count; number++)
        {
            var client = new Client
            {
                FirstName = $"Client{number}",
                LastName = "Test",
                PhoneNumber = $"+373600{number:D4}",
                PassportNumber = $"P{number:D6}",
                BirthDate = new DateOnly(1980 + number % 25, 1 + number % 12, 1 + number % 27)
            };
            clients.Add(client);
        }

        return clients;
    }

    public List<Client> CreateRandomClients(int count = 1000, int seed = 42) => new Faker<Client>().UseSeed(seed).RuleFor(c => c.Id, f => f.Random.Guid()).RuleFor(c => c.FirstName, f => f.Name.FirstName()).RuleFor(c => c.LastName, f => f.Name.LastName()).RuleFor(c => c.BirthDate, f => DateOnly.FromDateTime(f.Date.Between(new DateTime(1950, 1, 1), new DateTime(2005, 1, 1)))).RuleFor(c => c.PhoneNumber, f => $"+3736{f.IndexFaker:D7}").RuleFor(c => c.PassportNumber, f => f.Random.Guid().ToString("N")).Generate(count);
    public List<Employee> CreateRandomEmployees(int count = 1000, int seed = 42) => new Faker<Employee>().UseSeed(seed).RuleFor(e => e.Id, f => f.Random.Guid()).RuleFor(e => e.FirstName, f => f.Name.FirstName()).RuleFor(e => e.LastName, f => f.Name.LastName()).RuleFor(e => e.Position, f => f.Name.JobType()).RuleFor(e => e.Salary, f => f.Random.Int(1000, 10000)).RuleFor(e => e.BirthDate, f => DateOnly.FromDateTime(f.Date.Between(new DateTime(1950, 1, 1), new DateTime(2005, 1, 1)))).RuleFor(e => e.PhoneNumber, f => $"+3737{f.IndexFaker:D7}").RuleFor(e => e.PassportNumber, f => f.Random.Guid().ToString("N")).Generate(count);
    public Dictionary<string, Client> CreateClientsByPhone(int count = 1000) => CreateClients(count).ToDictionary(c => c.PhoneNumber);
    public List<Employee> CreateEmployees(int count = 1000)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var employees = new List<Employee>();
        for (int number = 1; number <= count; number++)
        {
            employees.Add(new Employee { FirstName = $"Employee{number}", LastName = "Test", Salary = 1000 + number, Position = "Bank specialist", BirthDate = new DateOnly(1980, 1, 1), PassportNumber = $"E{number}", PhoneNumber = $"+373700{number:D4}" });
        }

        return employees;
    }

    public Dictionary<Client, IReadOnlyList<Account>> CreateClientAccounts(int count = 1000)
    {
        var result = new Dictionary<Client, IReadOnlyList<Account>>();
        foreach (Client client in CreateClients(count))
        {
            var accounts = new List<Account>
            {
                new Account(),
                new Account()
            };
            result.Add(client, accounts);
        }

        return result;
    }

    public static (TimeSpan List, TimeSpan Dictionary) MeasurePhoneSearch(IReadOnlyList<Client> clients, IReadOnlyDictionary<string, Client> index, string phone)
    {
        var timer = Stopwatch.StartNew();
        _ = clients.LastOrDefault(x => x.PhoneNumber == phone);
        timer.Stop();
        var list = timer.Elapsed;
        timer.Restart();
        _ = index.TryGetValue(phone, out _);
        timer.Stop();
        return (list, timer.Elapsed);
    }
}
