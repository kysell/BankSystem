using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Npgsql;
using BankSystem.App.Dto;
using NUnit.Framework;

namespace BankSystem.App.Tests;

// Настоящий локальный сервер: проверяем маршруты, DTO, DI и JSON.
[NonParallelizable]
public class ApiIntegrationTests
{
    private Process? _server;
    private HttpClient _http = null!;
    private readonly StringBuilder _log = new();
    [OneTimeSetUp]
    public async Task Start()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(typeof(global::Program).Assembly.Location);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add($"http://127.0.0.1:{port}");
        string connectionString = Environment.GetEnvironmentVariable("BANKSYSTEM_HTTP_TEST_CONNECTION") ?? string.Empty;
        if (connectionString != string.Empty)
        {
            var databaseOptions = new NpgsqlConnectionStringBuilder(connectionString);
            if (Environment.GetEnvironmentVariable("BANKSYSTEM_ALLOW_TRAINING_DB") != "1" ||
                databaseOptions.Database == null || !databaseOptions.Database.StartsWith("banksystem_training_", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("HTTP database tests require an explicitly allowed banksystem_training_* database.");
            }
        }
        start.Environment["ConnectionStrings__BankSystem"] = connectionString;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        _server = Process.Start(start)!;
        _server.OutputDataReceived += (_, e) =>
        {
            lock (_log)
            {
                _log.AppendLine(e.Data);
            }
        };
        _server.ErrorDataReceived += (_, e) =>
        {
            lock (_log)
            {
                _log.AppendLine(e.Data);
            }
        };
        _server.BeginOutputReadLine();
        _server.BeginErrorReadLine();
        _http = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        try
        {
            for (var i = 0; i < 100; i++)
            {
                if (_server.HasExited)
                {
                    throw new InvalidOperationException(_log.ToString());
                }

                try
                {
                    using var response = await _http.GetAsync("/openapi/v1.json");
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch (HttpRequestException)
                {
                }

                await Task.Delay(100);
            }

            throw new TimeoutException(_log.ToString());
        }
        catch
        {
            Stop();
            throw;
        }
    }

    [OneTimeTearDown]
    public void Stop()
    {
        _http?.Dispose();
        if (_server is not null)
        {
            if (!_server.HasExited)
            {
                _server.Kill(entireProcessTree: true);
                _server.WaitForExit(5000);
            }

            _server.Dispose();
        }
    }

    [TestCase("clients")]
    [TestCase("employees")]
    public async Task Http_CrudAndErrorResponses(string resource)
    {
        var payload = new
        {
            firstName = "Ada",
            lastName = "Test",
            birthDate = "1990-01-01",
            phoneNumber = "123",
            passportNumber = "P123",
            position = "Developer",
            salary = 2000
        };
        using var created = await _http.PostAsJsonAsync($"/api/{resource}", payload);
        Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created), await created.Content.ReadAsStringAsync());
        var json = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetGuid();
        Assert.That(json.TryGetProperty("bonus", out _), Is.False);
        var uri = $"/api/{resource}/{id}";
        Assert.That(created.Headers.Location?.AbsolutePath, Is.EqualTo(uri));
        using var get = await _http.GetAsync(uri);
        Assert.That(get.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var invalid = await _http.PutAsJsonAsync(uri, payload with { birthDate = "2020-01-01" });
        Assert.That(invalid.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        using var unchanged = await _http.GetAsync(uri);
        Assert.That((await unchanged.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("birthDate").GetString(), Is.EqualTo("1990-01-01"));
        using var updated = await _http.PutAsJsonAsync(uri, payload with { firstName = "Updated" });
        Assert.That(updated.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        using var found = await _http.GetAsync($"/api/{resource}?name=Updated");
        Assert.That((await found.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().Any(e => e.GetProperty("id").GetGuid() == id), Is.True);
        using var deleted = await _http.DeleteAsync(uri);
        Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        using var missing = await _http.GetAsync(uri);
        Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        using var deleteMissing = await _http.DeleteAsync(uri);
        Assert.That(deleteMissing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        using var badRequest = await _http.PostAsJsonAsync($"/api/{resource}", payload with { firstName = "" });
        Assert.That(badRequest.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        using var badJson = await _http.PostAsync($"/api/{resource}", new StringContent("{broken", Encoding.UTF8, "application/json"));
        Assert.That(badJson.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task OpenApiAndSwaggerRoutes_AreAvailable()
    {
        var document = await _http.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        Assert.That(document.GetProperty("paths").TryGetProperty("/api/clients/{id}", out _), Is.True);
        Assert.That(document.GetProperty("paths").TryGetProperty("/api/employees/{id}", out _), Is.True);
        using var page = await _http.GetAsync("/swagger");
        Assert.That(page.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await page.Content.ReadAsStringAsync(), Does.Contain("swagger-ui-bundle.js"));
        using var script = await _http.GetAsync("/swagger/swagger-ui-bundle.js");
        Assert.That(script.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await script.Content.ReadAsStringAsync(), Does.Contain("SwaggerUIBundle"));
        string settings = await _http.GetStringAsync("/swagger/index.js");
        Assert.That(settings, Does.Contain("/openapi/v1.json"));
    }
}
