using System.Net;
using System.Text;
using BankSystem.App.Interfaces;
using BankSystem.App.Services;
using BankSystem.Data.Storages;
using BankSystem.Domain.Models;
using NUnit.Framework;

namespace BankSystem.App.Tests;

public class BoundaryTests
{
    [Test]
    public void Client_AccountCrudValidatesOwnerCurrencyDuplicatesAndFilterRange()
    {
        var storage = new ClientStorage();
        var service = new ClientService(storage);
        var client = RegressionTests.Adult();
        service.AddClient(client);
        var account = new Account
        {
            Currency = new("EUR", .9m)
        };
        service.AddAccount(client.Id, account);
        Assert.Throws<InvalidOperationException>(() => service.AddAccount(client.Id, account));
        var updated = new Account
        {
            Id = account.Id,
            Currency = new("GBP", .8m)
        };
        updated.Deposit(100);
        service.UpdateAccount(client.Id, updated);
        Assert.That(client.Accounts.Single(a => a.Id == account.Id).Amount, Is.EqualTo(100));
        Assert.Throws<ArgumentException>(() => service.AddAccount(client.Id, new Account { Currency = new("BAD_CODE", 0) }));
        Assert.Throws<KeyNotFoundException>(() => service.UpdateAccount(Guid.NewGuid(), updated));
        Assert.Throws<KeyNotFoundException>(() => service.UpdateAccount(client.Id, new Account()));
        service.DeleteAccount(client.Id, account.Id);
        Assert.Throws<KeyNotFoundException>(() => service.DeleteAccount(client.Id, account.Id));
        Assert.That(service.Search(new ClientFilter("ada", "123", "P123", new(1989, 1, 1), new(1991, 1, 1))), Has.Count.EqualTo(1));
        Assert.That(service.Search(new ClientFilter(PhoneNumber: "missing")), Is.Empty);
        Assert.Throws<ArgumentException>(() => service.Search(new ClientFilter(BornFrom: new(2000, 1, 1), BornTo: new(1990, 1, 1))));
    }

    [Test]
    public void Employee_ServiceRejectsInvalidSalaryPassportAndMinor()
    {
        var service = new EmployeeService(new EmployeeStorage());
        var employee = new TestDataGenerator().CreateEmployees(1).Single();
        employee.Position = "Developer";
        employee.Salary = -1;
        Assert.Throws<ArgumentException>(() => service.Add(employee));
        employee.Salary = 1;
        employee.PassportNumber = null;
        Assert.Throws<ArgumentException>(() => service.Add(employee));
        employee.PassportNumber = "P";
        employee.BirthDate = new(2020, 1, 1);
        Assert.Throws<ArgumentException>(() => service.Add(employee));
    }

    [Test]
    public void Account_TwoActualThreadsDepositWithoutLostUpdates()
    {
        var account = new Account();
        using var gate = new Barrier(2);
        void Deposit()
        {
            gate.SignalAndWait();
            for (var i = 0; i < 10; i++)
            {
                account.Deposit(100);
            }
        }

        var first = new Thread(Deposit);
        var second = new Thread(Deposit);
        first.Start();
        second.Start();
        first.Join();
        second.Join();
        Assert.That(account.Amount, Is.EqualTo(2000));
        Assert.Throws<InvalidOperationException>(() => account.Withdraw(2001));
        Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(0));
    }

    [Test]
    public void ValueType_MethodByValueDoesNotMutateOriginal()
    {
        var currency = new CurrencyValue("USD", 1);
        new BankService().UpdateCurrency(currency, "EUR", .9m);
        Assert.That(currency.Code, Is.EqualTo("USD"));
    }

    [Test]
    public void Generator_DictionaryWithSeveralAccountsFindsEquivalentKey()
    {
        var generator = new TestDataGenerator();
        var random = generator.CreateRandomClients();
        Assert.That(random.Select(c => c.PhoneNumber).Distinct().Count(), Is.EqualTo(1000));
        Assert.That(random.Select(c => c.FirstName).Distinct().Count(), Is.GreaterThan(1));
        Assert.That(generator.CreateRandomEmployees(), Has.Count.EqualTo(1000));
        var accounts = new TestDataGenerator().CreateClientAccounts();
        var client = accounts.Keys.First();
        Assert.That(accounts[new Client { Id = client.Id }], Has.Count.EqualTo(2));
        Assert.That(new TestDataGenerator().CreateClientsByPhone(), Has.Count.EqualTo(1000));
        Assert.That(new TestDataGenerator().CreateEmployees(), Has.Count.EqualTo(1000));
    }

    [Test]
    public async Task HttpServices_PropagateFailuresAndDoNotSendUnauthorizedRequests()
    {
        var count = 0;
        using var http = new HttpClient(new StubHandler(_ =>
        {
            count++;
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }))
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var team = new TeamService(http);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await team.GetTeamsAsync());
        Assert.That(count, Is.Zero);
        Assert.ThrowsAsync<HttpRequestException>(async () => await team.AuthorizeAsync(new("user", "placeholder")));
        Assert.ThrowsAsync<HttpRequestException>(async () => await new CurrencyService(http).ConvertAsync(1.25m, "USD", "EUR"));
        Assert.ThrowsAsync<ArgumentException>(async () => await new CurrencyService(http).ConvertAsync(1, "?bad", "EUR"));
        var same = await new CurrencyService(http).ConvertAsync(1, "USD", "usd");
        Assert.That(same.ConvertedAmount, Is.EqualTo(1));
    }

    [Test]
    public async Task TeamService_AddPostsBearerTokenAndPayload()
    {
        HttpRequestMessage? sent = null;
        using var http = new HttpClient(new StubHandler(r =>
        {
            sent = r;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(r.RequestUri!.AbsolutePath.EndsWith("SignIn") ? "{\"name\":\"Test\",\"avatarUrl\":\"\",\"token\":\"test\"}" : "{}", Encoding.UTF8, "application/json")
            };
        }))
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new TeamService(http);
        await service.AuthorizeAsync(new("user", "placeholder"));
        await service.AddAsync(new(1, "Team", 2000, "D", "C", ""));
        Assert.That(sent!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(sent.Headers.Authorization!.Parameter, Is.EqualTo("test"));
        Assert.That(sent.RequestUri!.AbsolutePath, Is.EqualTo("/api/Team/Add"));
    }
}
