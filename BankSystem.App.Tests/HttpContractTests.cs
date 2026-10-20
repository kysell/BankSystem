using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BankSystem.App.Dto;
using BankSystem.App.Services;
using BankSystem.Data.Storages;
using BankSystem.Domain.Models;
using NUnit.Framework;

namespace BankSystem.App.Tests;

public class HttpContractTests
{
    [Test]
    public async Task Teams_UsesIntegerFoundationYearFromPublishedDexSchema()
    {
        string? sentBody = null;
        var handler = new AsyncHandler(async request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("SignIn"))
            {
                return Json("{\"name\":\"Test\",\"avatarUrl\":\"\",\"token\":\"test-token\"}");
            }

            if (request.Method == HttpMethod.Post)
            {
                sentBody = await request.Content!.ReadAsStringAsync();
                return Json("{}");
            }

            return Json("{\"data\":[{\"id\":1,\"name\":\"Team\",\"foundationYear\":2000,\"division\":\"D\",\"conference\":\"C\",\"imageUrl\":\"\"}],\"count\":1,\"page\":1,\"size\":10}");
        });
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new TeamService(http);
        await service.AuthorizeAsync(new AuthData("test", "placeholder"));
        await service.AddAsync(new Team(0, "Team", 2000, "D", "C", ""));
        using JsonDocument body = JsonDocument.Parse(sentBody!);
        Assert.That(body.RootElement.GetProperty("foundationYear").GetInt32(), Is.EqualTo(2000));
        TeamResponse teams = await service.GetTeamsAsync();
        Assert.That(teams.Data.Single().FoundationYear, Is.EqualTo(2000));
    }

    [Test]
    public async Task Teams_FailedReauthorizationClearsOldToken()
    {
        int signInCount = 0;
        var handler = new StubHandler(_ =>
        {
            signInCount++;
            if (signInCount == 1)
            {
                return Json("{\"name\":\"Test\",\"avatarUrl\":\"\",\"token\":\"old-token\"}");
            }

            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new TeamService(http);
        await service.AuthorizeAsync(new AuthData("test", "placeholder"));
        Assert.ThrowsAsync<HttpRequestException>(async () => await service.AuthorizeAsync(new AuthData("test", "placeholder")));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await service.GetTeamsAsync());
        Assert.That(signInCount, Is.EqualTo(2));
    }

    [Test]
    public void Teams_RejectsPlainHttpBeforeSendingCredentials()
    {
        using var http = new HttpClient(new StubHandler(_ => throw new AssertionException("Request must not be sent.")))
        {
            BaseAddress = new Uri("http://example.test/")
        };
        Assert.Throws<ArgumentException>(() => new TeamService(http));
    }

    [Test]
    public void Account_InvalidCurrencyThrowsValidationError()
    {
        var service = new ClientService(new ClientStorage());
        var account = new Account
        {
            Currency = null!
        };
        Assert.Throws<ArgumentException>(() => service.AddAccount(Guid.NewGuid(), account));
    }

    [Test]
    public void Employees_ReversedBirthDateFilterIsRejected()
    {
        var service = new EmployeeService(new EmployeeStorage());
        var filter = new ClientFilter(BornFrom: new DateOnly(2000, 1, 1), BornTo: new DateOnly(1990, 1, 1));
        Assert.ThrowsAsync<ArgumentException>(async () => await service.SearchResponsesAsync(filter));
    }

    private static HttpResponseMessage Json(string value)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(value, Encoding.UTF8, "application/json")
        };
    }

    private sealed class AsyncHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handle;
        public AsyncHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handle)
        {
            _handle = handle;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => _handle(request);
    }
}
