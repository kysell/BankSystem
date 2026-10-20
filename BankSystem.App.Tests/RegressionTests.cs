using System.Text.Json;
using BankSystem.App.Interfaces;
using BankSystem.App.Services;
using BankSystem.Data.Storages;
using BankSystem.Domain.Models;
using ExportTool;
using NUnit.Framework;

namespace BankSystem.App.Tests;

public class RegressionTests
{
    internal static Client Adult(string name = "Ada") => new()
    {
        FirstName = name,
        LastName = "Test",
        BirthDate = new(1990, 1, 1),
        PhoneNumber = "123",
        PassportNumber = "P123"
    };
    [TestCase("Ada\"Jr")]
    [TestCase("Ada, Jr")]
    [TestCase("Ада\nЛавлейс")]
    [TestCase("Ада\r\nЛавлейс")]
    public async Task Csv_PreservesQuotedAndMultilineNames(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), $"csv-{Guid.NewGuid()}.csv");
        try
        {
            var service = new ExportService();
            await service.ExportClientsToCsvAsync([Adult(name)], path);
            Assert.That((await service.ImportClientsFromCsvAsync(path)).Single().FirstName, Is.EqualTo(name));
            await service.ExportClientsToCsvAsync([], path);
            Assert.That(await service.ImportClientsFromCsvAsync(path), Is.Empty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestCase("wrong,header\n")]
    [TestCase("Id,FirstName,LastName,BirthDate,PhoneNumber,PassportNumber\n\"unclosed\n")]
    public async Task Csv_RejectsMalformedInput(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"csv-{Guid.NewGuid()}.csv");
        try
        {
            await File.WriteAllTextAsync(path, content);
            Assert.ThrowsAsync<FormatException>(async () => await new ExportService().ImportClientsFromCsvAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task Json_RestoresAccountsBalancesCurrencyAndEmployee()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"export-{Guid.NewGuid()}");
        try
        {
            var client = Adult();
            var account = new Account
            {
                Currency = new("EUR", .9m)
            };
            account.Deposit(123.45m);
            client.AddAccount(account);
            var exporter = new ExportService();
            var path = Path.Combine(directory, "client.json");
            await exporter.ExportJsonAsync(client, path);
            var restored = (await exporter.ImportJsonAsync<Client>(path))!;
            Assert.That(restored.Accounts.Single().Amount, Is.EqualTo(123.45m));
            Assert.That(restored.Accounts.Single().Id, Is.EqualTo(account.Id));
            Assert.That(restored.Accounts.Single().Currency.Code, Is.EqualTo("EUR"));
            await exporter.ExportJsonAsync(new Employee { Salary = 42, Contract = "contract" }, path);
            Assert.That((await exporter.ImportJsonAsync<Employee>(path))!.Salary, Is.EqualTo(42));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Test]
    public async Task Csv_ImportIntoServiceCreatesDefaultAccount()
    {
        var path = Path.Combine(Path.GetTempPath(), $"csv-{Guid.NewGuid()}.csv");
        try
        {
            var storage = new ClientStorage();
            var exporter = new ExportService();
            await exporter.ExportClientsToCsvAsync([Adult()], path);
            await exporter.ImportClientsIntoStorageAsync(path, new ClientService(storage));
            Assert.That(storage.Get().Single().Accounts.Single().Currency.Code, Is.EqualTo("USD"));
        }
        finally
        {
            File.Delete(path);
        }
    }


    [Test]
    public async Task MonthlyInterest_HandlesZeroBalancesAndPersistsInterest()
    {
        var storage = new ClientStorage();
        var client = Adult();
        var account = new Account();
        account.Deposit(100);
        client.AddAccount(account);
        client.AddAccount(new Account());
        storage.Add(client);
        var updater = new RateUpdaterService(storage);
        await updater.ApplyMonthlyRateAsync(0);
        await updater.ApplyMonthlyRateAsync(5);
        Assert.That(account.Amount, Is.EqualTo(105));
        Assert.That(client.Accounts.Last().Amount, Is.Zero);
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await updater.ApplyMonthlyRateAsync(-1));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.That(Assert.CatchAsync(async () => await updater.RunAsync(1, token: cts.Token)), Is.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void Employee_EqualityFindsEquivalentInstanceInList()
    {
        var first = new Employee();
        Assert.That(new List<Employee> { first }.Contains(new Employee { Id = first.Id }), Is.True);
        Assert.That(first.GetHashCode(), Is.EqualTo(new Employee { Id = first.Id }.GetHashCode()));
    }

    [Test]
    public void Employees_StorageSupportsAgeQueriesAndRejectsDuplicates()
    {
        var storage = new EmployeeStorage();
        var employees = new TestDataGenerator().CreateEmployees(3);
        employees[1].BirthDate = new(2000, 1, 1);
        foreach (var employee in employees)
        {
            storage.Add(employee);
        }

        Assert.That(storage.Get().MaxBy(e => e.BirthDate), Is.SameAs(employees[1]));
        Assert.That(storage.Get().MinBy(e => e.BirthDate)!.BirthDate.Year, Is.EqualTo(1980));
        Assert.That(storage.Get().Average(e => e.Age), Is.InRange(20, 60));
        Assert.Throws<InvalidOperationException>(() => storage.Add(employees[0]));
    }

    [Test]
    public void Bank_ConversionSalaryAndBlacklist()
    {
        var service = new BankService();
        var client = Adult();
        var employee = service.ConvertClientToEmployee(client);
        Assert.That(employee.Id, Is.EqualTo(client.Id));
        Assert.That(employee.PassportNumber, Is.EqualTo(client.PassportNumber));
        employee.IsBankOwner = true;
        Assert.That(service.CalculateOwnersSalary(105, 4, [employee, new Employee { IsBankOwner = true }]), Is.EqualTo(50));
        Assert.Throws<InvalidOperationException>(() => service.CalculateOwnersSalary(1, 0, []));
        service.AddBonus(client, 10);
        service.AddToBlackList(client);
        Assert.That(client.Bonus, Is.EqualTo(10));
        Assert.That(service.IsPersonInBlackList(client), Is.True);
    }

    [Test]
    public void Events_NotifyChangesZeroTransitionAndUnsubscribe()
    {
        var balance = new ObservableBalance();
        var calls = 0;
        System.ComponentModel.PropertyChangedEventHandler handler = (_, _) => calls++;
        balance.PropertyChanged += handler;
        balance.Amount = 5;
        balance.Amount = 5;
        balance.PropertyChanged -= handler;
        balance.Amount = 6;
        Assert.That(calls, Is.EqualTo(1));
        var analyzer = new NumberStreamAnalyzer(10);
        var changes = 0;
        analyzer.DifferenceExceeded += (_, _) => changes++;
        analyzer.Add(0);
        analyzer.Add(1);
        analyzer.Add(1.05m);
        analyzer.Add(2);
        Assert.That(changes, Is.EqualTo(2));
    }

    [Test]
    public void Reflection_CreatesByNameAndInvokesVoidAndNullMembers()
    {
        var target = ReflectionTools.Create(typeof(ReflectionRegressionTarget).AssemblyQualifiedName!);
        Assert.That(ReflectionTools.Invoke(target, "DoWork"), Is.Null);
        Assert.That(ReflectionTools.GetPrivateProperty(target, "Empty"), Is.Null);
        Assert.Throws<MissingMethodException>(() => ReflectionTools.Invoke(target, "Missing"));
    }

}

public sealed class ReflectionRegressionTarget
{
    private string? Empty => null;

    public void DoWork()
    {
    }
}
