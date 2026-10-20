using BankSystem.App.Interfaces;
using BankSystem.App.Services;
using BankSystem.Data;
using BankSystem.Data.Storages;
using BankSystem.Domain.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BankSystem.App.Tests;

public class ClientDbStorageTests
{
    private SqliteConnection _connection = null!;
    private BankSystemDbContext _context = null!;
    private ClientDbStorage _storage = null!;
    [SetUp]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _context = new BankSystemDbContext(new DbContextOptionsBuilder<BankSystemDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
        _storage = new ClientDbStorage(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _storage.Dispose();
        _context.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task Clients_CrudAndAccountsPersistAcrossTrackingReset()
    {
        var client = RegressionTests.Adult();
        await _storage.AddAsync(client);
        _context.ChangeTracker.Clear();
        Assert.That((await _storage.GetByIdAsync(client.Id))!.Accounts.Single().Currency.Code, Is.EqualTo("USD"));
        var account = new Account
        {
            Currency = new("EUR", .9m)
        };
        account.Deposit(100);
        await _storage.AddAccountAsync(client.Id, account);
        _context.ChangeTracker.Clear();
        await _storage.WithdrawAsync(client.Id, account.Id, 20);
        _context.ChangeTracker.Clear();
        Assert.That((await _storage.GetByIdAsync(client.Id))!.Accounts.Single(a => a.Id == account.Id).Amount, Is.EqualTo(80));
        var replacement = RegressionTests.Adult("Changed");
        replacement = new Client
        {
            Id = client.Id,
            FirstName = replacement.FirstName,
            LastName = replacement.LastName,
            BirthDate = replacement.BirthDate,
            PhoneNumber = replacement.PhoneNumber,
            PassportNumber = replacement.PassportNumber
        };
        await _storage.UpdateAsync(replacement);
        Assert.That((await _storage.GetByIdAsync(client.Id))!.Accounts, Has.Count.EqualTo(2));
        await _storage.DeleteAccountAsync(client.Id, account.Id);
        await _storage.DeleteAsync(client);
        _context.ChangeTracker.Clear();
        Assert.That(await _storage.GetByIdAsync(client.Id), Is.Null);
        Assert.That(await _context.Accounts.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task Accounts_RejectCrossClientUpdateAndDelete()
    {
        var first = RegressionTests.Adult();
        var second = RegressionTests.Adult("Other");
        await _storage.AddAsync(first);
        await _storage.AddAsync(second);
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _storage.UpdateAccountAsync(first.Id, second.Accounts.Single()));
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _storage.DeleteAccountAsync(first.Id, second.Accounts.Single().Id));
        Assert.That(await _context.Accounts.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public void Database_EnforcesRequiredOwnerForeignKeyAndNonnegativeBalance()
    {
        _context.Accounts.Add(new Account());
        Assert.Throws<DbUpdateException>(() => _context.SaveChanges());
        _context.ChangeTracker.Clear();
        var client = RegressionTests.Adult();
        _storage.Add(client);
        Assert.Throws<SqliteException>(() => _context.Database.ExecuteSqlRaw("UPDATE account SET amount = -1"));
    }

    [Test]
    public async Task Search_FiltersInDatabaseAndPaginates()
    {
        var generator = new TestDataGenerator();
        foreach (var client in generator.CreateClients(12))
        {
            _storage.Add(client);
        }

        _context.ChangeTracker.Clear();
        var service = new ClientService(_storage);
        Assert.That(await service.SearchAsync(new ClientFilter(FullName: "client1")), Has.Count.EqualTo(4));
        var page = _storage.Search(null, 2, 5);
        Assert.That(page.Items, Has.Count.EqualTo(5));
        Assert.That(page.TotalCount, Is.EqualTo(12));
        Assert.Throws<ArgumentOutOfRangeException>(() => _storage.Search(null, 0, 5));
        Assert.That(_context.Clients.Where(c => c.FirstName.Contains("1")).OrderBy(c => c.Id).Take(2).ToQueryString(), Does.Contain("LIMIT"));
        Assert.That(await _context.Clients.GroupBy(c => c.BirthDate.Year).Select(g => new { Year = g.Key, Count = g.Count() }).CountAsync(), Is.EqualTo(12));
    }

    [Test]
    public async Task Employees_AsyncCrudAndCancellation()
    {
        IEmployeeStorage storage = new EmployeeDbStorage(_context);
        var employee = new TestDataGenerator().CreateEmployees(1).Single();
        await storage.AddAsync(employee);
        _context.ChangeTracker.Clear();
        var loaded = await storage.GetByIdAsync(employee.Id);
        loaded!.Salary = 99;
        await storage.UpdateAsync(loaded);
        _context.ChangeTracker.Clear();
        Assert.That((await storage.GetByIdAsync(employee.Id))!.Salary, Is.EqualTo(99));
        await storage.DeleteAsync(loaded);
        Assert.That(await storage.GetAsync(), Is.Empty);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.That(Assert.CatchAsync(async () => await storage.GetAsync(token: cts.Token)), Is.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task ConcurrentContexts_RejectStaleBalanceWithoutLosingMoney()
    {
        var client = RegressionTests.Adult();
        var account = new Account();
        account.Deposit(100);
        client.AddAccount(account);
        await _storage.AddAsync(client);
        using var other = new BankSystemDbContext(new DbContextOptionsBuilder<BankSystemDbContext>().UseSqlite(_connection).Options);
        using var otherStorage = new ClientDbStorage(other);
        var stale = (await otherStorage.GetByIdAsync(client.Id))!.Accounts.Single();
        account.Deposit(50);
        await _storage.UpdateAccountAsync(client.Id, account);
        stale.Withdraw(80);
        Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => await otherStorage.UpdateAccountAsync(client.Id, stale));
        _context.ChangeTracker.Clear();
        Assert.That((await _storage.GetByIdAsync(client.Id))!.Accounts.Single().Amount, Is.EqualTo(150));
    }

    [Test]
    public async Task CsvImport_ReallyPersistsClientAndDefaultAccount()
    {
        var path = Path.Combine(Path.GetTempPath(), $"db-import-{Guid.NewGuid()}.csv");
        try
        {
            var exporter = new ExportTool.ExportService();
            var client = RegressionTests.Adult();
            await exporter.ExportClientsToCsvAsync([client], path);
            await exporter.ImportClientsIntoStorageAsync(path, new ClientService(_storage));
            _context.ChangeTracker.Clear();
            Assert.That((await _storage.GetByIdAsync(client.Id))!.Accounts.Single().Currency.Code, Is.EqualTo("USD"));
            Assert.That(await _context.Currencies.CountAsync(), Is.EqualTo(1));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task Interest_UpdatesDetachedAccountsInDatabase()
    {
        var client = RegressionTests.Adult();
        var account = new Account();
        account.Deposit(100);
        client.AddAccount(account);
        await _storage.AddAsync(client);
        _context.ChangeTracker.Clear();
        await new RateUpdaterService(_storage).ApplyMonthlyRateAsync(10);
        _context.ChangeTracker.Clear();
        Assert.That((await _storage.GetByIdAsync(client.Id))!.Accounts.Single().Amount, Is.EqualTo(110));
    }
}
