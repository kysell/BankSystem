using BankSystem.App.Services;
using BankSystem.App.Dto;
using BankSystem.Api.Controllers;
using BankSystem.App.Interfaces;
using BankSystem.Data;
using BankSystem.Data.Storages;
using BankSystem.Domain.Models;
using ExportTool;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Net;
using System.Text;

namespace BankSystem.App.Tests;

public class ClientServiceTests
{
    private static Client Adult() => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        BirthDate = new DateOnly(1990, 1, 1),
        PhoneNumber = "+3736000001",
        PassportNumber = "P1"
    };
    [Test]
    public void AddClient_ValidClient_AddsDefaultDollarAccount()
    {
        var storage = new ClientStorage();
        var client = Adult();
        new ClientService(storage).AddClient(client);
        Assert.That(storage.GetById(client.Id), Is.SameAs(client));
        Assert.That(client.Accounts, Has.Count.EqualTo(1));
        Assert.That(client.Accounts[0].Currency.Code, Is.EqualTo("USD"));
    }

    [Test]
    public void AddClient_Underage_ThrowsDomainException()
    {
        var client = Adult();
        client.BirthDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-17);
        Assert.That(() => new ClientService(new ClientStorage()).AddClient(client), Throws.TypeOf<UnderageClientException>());
    }

    [Test]
    public void AddClient_WithoutPassport_Throws()
    {
        var client = Adult();
        client.PassportNumber = null;
        Assert.That(() => new ClientService(new ClientStorage()).AddClient(client), Throws.ArgumentException);
    }

    [Test]
    public async Task WithdrawManyAsync_ConcurrentRequests_ChangesAccountSafely()
    {
        var storage = new ClientStorage();
        var client = Adult();
        var account = new Account();
        account.Deposit(500);
        client.AddAccount(account);
        new ClientService(storage).AddClient(client);
        await new ClientService(storage).WithdrawManyAsync(Enumerable.Repeat((client.Id, account.Id, 100m), 3));
        Assert.That(account.Amount, Is.EqualTo(200));
    }
}

public class EquivalenceTests
{
    [Test]
    public void GetHashCodeNecessityPositiveTest_EqualClientKeyFindsAccount()
    {
        var generator = new TestDataGenerator();
        Dictionary<Client, IReadOnlyList<Account>> accounts = generator.CreateClientAccounts(1);
        Client first = accounts.Keys.First();
        var equalButNew = new Client
        {
            Id = first.Id,
            FirstName = first.FirstName,
            LastName = first.LastName,
            BirthDate = first.BirthDate,
            PhoneNumber = first.PhoneNumber,
            PassportNumber = first.PassportNumber
        };
        Assert.That(ReferenceEquals(first, equalButNew), Is.False);
        Assert.That(accounts.TryGetValue(equalButNew, out var found), Is.True);
        Assert.That(found, Is.SameAs(accounts[first]));
        Assert.That(found, Has.Count.EqualTo(2));
    }

    [Test]
    public void Client_CanHoldMultipleAccounts()
    {
        var client = new Client();
        client.AddAccount(new Account());
        client.AddAccount(new Account());
        Assert.That(client.Accounts, Has.Count.EqualTo(2));
    }
}

public class LanguageFeatureTests
{
    [Test]
    public void Extensions_CreateTimeSpan()
    {
        Assert.That(2.Seconds(), Is.EqualTo(TimeSpan.FromSeconds(2)));
    }

    [Test]
    public void Regex_ValidatesFindsAndReplaces()
    {
        Assert.That(TextPatterns.IsSocialSecurityNumber("111-22-3333"), Is.True);
        Assert.That(TextPatterns.FindDuplicateWords("one one two"), Is.EqualTo(new[] { "one" }));
        Assert.That(TextPatterns.ReplaceMoney("Cost 10.00"), Is.EqualTo("Cost $10.00"));
    }

    [Test]
    public void Queue_RaisesThresholdAndEmptyEvents()
    {
        var queue = new ThresholdQueue<int>(1);
        var threshold = false;
        var empty = false;
        queue.ThresholdReached += (_, _) => threshold = true;
        queue.BecameEmpty += (_, _) => empty = true;
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Dequeue();
        queue.Dequeue();
        Assert.That(threshold && empty, Is.True);
    }

    [Test]
    public void BankService_UpdatesStructByRefAndEmployeeContract()
    {
        var service = new BankService();
        var currency = new CurrencyValue("USD", 1);
        service.UpdateCurrency(ref currency, "EUR", .9m);
        var employee = new Employee
        {
            FirstName = "Ada",
            Position = "Dev",
            Salary = 1
        };
        service.UpdateEmployeeContract(employee);
        Assert.That(currency.Code, Is.EqualTo("EUR"));
        Assert.That(employee.Contract, Does.Contain("Ada"));
    }

    [Test]
    public void Circle_CompareTo_SortsByArea()
    {
        var circles = new[]
        {
            new Circle(3),
            new Circle(1),
            new Circle(2)
        };
        Array.Sort(circles);
        Assert.That(circles.Select(x => x.Radius), Is.EqualTo(new[] { 1d, 2d, 3d }));
    }

    [Test]
    public void Circle_Comparer_SortsByAreaDescending()
    {
        var circles = new[]
        {
            new Circle(1),
            new Circle(3),
            new Circle(2)
        };
        Array.Sort(circles, new CircleAreaDescendingComparer());
        Assert.That(circles.Select(x => x.Radius), Is.EqualTo(new[] { 3d, 2d, 1d }));
    }

    [Test]
    public void Storage_LinqQueries_ReturnYoungestOldestAndAverageAge()
    {
        var storage = new ClientStorage();
        var first = new Client
        {
            BirthDate = new DateOnly(1980, 1, 1)
        };
        var second = new Client
        {
            BirthDate = new DateOnly(2000, 1, 1)
        };
        storage.Add(first);
        storage.Add(second);
        var clients = storage.Get();
        Assert.That(clients.MaxBy(x => x.BirthDate), Is.SameAs(second));
        Assert.That(clients.MinBy(x => x.BirthDate), Is.SameAs(first));
        Assert.That(clients.Average(x => x.Age), Is.GreaterThan(0));
    }

    [Test]
    public void Delegates_FilterAndRemoveHandler_Work()
    {
        Assert.That(DelegateExamples.Apply(x => x * 2, 3), Is.EqualTo(6));
        Assert.That(DelegateExamples.Filter([1, 2, 3], x => x > 1), Is.EqualTo([2, 3]));
        var calls = 0;
        DelegateExamples.InvokeThenRemove(() => calls++, () => calls += 10);
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void Reflection_CreatesInvokesAndReadsPrivateMember()
    {
        var target = new ReflectionTarget();
        Assert.That(ReflectionTools.Invoke(target, "Multiply", 3), Is.EqualTo(6));
        Assert.That(ReflectionTools.GetPrivateProperty(target, "Secret"), Is.EqualTo("value"));
    }
}


public class ExportTests
{
    [Test]
    public async Task JsonExport_Client_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), $"client-{Guid.NewGuid()}.json");
        try
        {
            var source = new Client
            {
                FirstName = "Ada",
                LastName = "Lovelace"
            };
            var service = new ExportService();
            await service.ExportJsonAsync(source, path);
            var result = await service.ImportJsonAsync<Client>(path);
            Assert.That(result?.FirstName, Is.EqualTo("Ada"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public async Task CsvExport_Clients_ImportsRows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clients-{Guid.NewGuid()}.csv");
        try
        {
            var original = new Client
            {
                FirstName = "Ada, Jr.",
                LastName = "Lovelace",
                BirthDate = new DateOnly(1990, 1, 1),
                PhoneNumber = "1",
                PassportNumber = "P"
            };
            var service = new ExportService();
            await service.ExportClientsToCsvAsync([original], path);
            var result = await service.ImportClientsFromCsvAsync(path);
            Assert.That(result.Single().Id, Is.EqualTo(original.Id));
            Assert.That(result.Single().FirstName, Is.EqualTo("Ada, Jr."));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}

public class InfrastructureAndHttpTests
{
    [Test]
    public void EfModel_MapsTablesForeignKeysAndCurrencyEntityWithoutDatabase()
    {
        var options = new DbContextOptionsBuilder<BankSystemDbContext>().UseNpgsql("Host=localhost;Database=not_used;Username=not_used;Password=not_used").Options;
        using var context = new BankSystemDbContext(options);
        var client = context.Model.FindEntityType(typeof(Client))!;
        Assert.That(client.GetTableName(), Is.EqualTo("client"));
        Assert.That(context.Model.FindEntityType(typeof(Account))!.GetForeignKeys(), Has.Count.EqualTo(2));
        Assert.That(context.Model.FindEntityType(typeof(Currency))!.GetTableName(), Is.EqualTo("currency"));
    }

    [Test]
    public async Task CurrencyService_UsesHttpClientAndMapsResponse()
    {
        using var client = new HttpClient(new StubHandler(request =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/v2/rate/usd/eur"));
            return Json("{\"date\":\"2026-10-20\",\"base\":\"USD\",\"quote\":\"EUR\",\"rate\":0.9}");
        }))
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new CurrencyService(client);
        var result = await service.ConvertAsync(2, "USD", "EUR");
        Assert.That(result, Is.EqualTo(1.8m));
    }

    [Test]
    public async Task ClientsController_CreateAndGet_ReturnsCreatedAndOk()
    {
        var storage = new ClientStorage();
        var controller = new ClientsController(new ClientService(storage));
        var created = (await controller.Create(new ClientRequest("Ada", "Lovelace", new DateOnly(1990, 1, 1), "+3736000001", "P1"), default)).Result as CreatedAtActionResult;
        Assert.That(created, Is.Not.Null);
        var client = (ClientResponse)created!.Value!;
        Assert.That((await controller.Get(client.Id, default)).Result, Is.TypeOf<OkObjectResult>());
    }

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };
}

public sealed class ReflectionTarget
{
    public int Visible => 1;
    private string Secret => "value";

    private int Multiply(int value) => value * 2;
}

public sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}
