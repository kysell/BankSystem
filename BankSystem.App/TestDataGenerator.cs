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

    public List<Client> CreateRandomClients(int count = 1000, int seed = 42)
    {
        var generator = new Faker<Client>();
        generator.UseSeed(seed);
        generator.RuleFor(client => client.Id, fake => fake.Random.Guid());
        generator.RuleFor(client => client.FirstName, fake => fake.Name.FirstName());
        generator.RuleFor(client => client.LastName, fake => fake.Name.LastName());
        generator.RuleFor(client => client.BirthDate, fake => DateOnly.FromDateTime(
            fake.Date.Between(new DateTime(1950, 1, 1), new DateTime(2005, 1, 1))));
        generator.RuleFor(client => client.PhoneNumber, fake => $"+3736{fake.IndexFaker:D7}");
        generator.RuleFor(client => client.PassportNumber, fake => fake.Random.Guid().ToString("N"));
        return generator.Generate(count);
    }

    public List<Employee> CreateRandomEmployees(int count = 1000, int seed = 42)
    {
        var generator = new Faker<Employee>();
        generator.UseSeed(seed);
        generator.RuleFor(employee => employee.Id, fake => fake.Random.Guid());
        generator.RuleFor(employee => employee.FirstName, fake => fake.Name.FirstName());
        generator.RuleFor(employee => employee.LastName, fake => fake.Name.LastName());
        generator.RuleFor(employee => employee.Position, fake => fake.Name.JobType());
        generator.RuleFor(employee => employee.Salary, fake => fake.Random.Int(1000, 10000));
        generator.RuleFor(employee => employee.BirthDate, fake => DateOnly.FromDateTime(
            fake.Date.Between(new DateTime(1950, 1, 1), new DateTime(2005, 1, 1))));
        generator.RuleFor(employee => employee.PhoneNumber, fake => $"+3737{fake.IndexFaker:D7}");
        generator.RuleFor(employee => employee.PassportNumber, fake => fake.Random.Guid().ToString("N"));
        return generator.Generate(count);
    }

    public Dictionary<string, Client> CreateClientsByPhone(int count = 1000)
    {
        var result = new Dictionary<string, Client>();
        foreach (Client client in CreateClients(count))
        {
            result.Add(client.PhoneNumber, client);
        }
        return result;
    }
    public List<Employee> CreateEmployees(int count = 1000)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var employees = new List<Employee>();
        for (int number = 1; number <= count; number++)
        {
            employees.Add(new Employee
            {
                FirstName = $"Employee{number}",
                LastName = "Test",
                Salary = 1000 + number,
                Position = "Bank specialist",
                BirthDate = new DateOnly(1980, 1, 1),
                PassportNumber = $"E{number}",
                PhoneNumber = $"+373700{number:D4}"
            });
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
