using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BankSystem.App.Services;

public sealed record AuthData(string Login, string Password);
public sealed record AuthResponse(string Name, string AvatarUrl, string Token);
public sealed record Team(int Id, string Name, int FoundationYear, string Division, string Conference, string ImageUrl);
public sealed record TeamResponse(IReadOnlyList<Team> Data, int Count, int Page, int Size);
public sealed class TeamService
{
    private readonly HttpClient _client;
    private string? _token;
    public TeamService(HttpClient client)
    {
        if (client.BaseAddress == null || client.BaseAddress.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("Team API requires an HTTPS base address.", nameof(client));
        }

        _client = client;
    }

    public async Task<AuthResponse> AuthorizeAsync(AuthData data, CancellationToken token = default)
    {
        // Не используем старый токен, если новая авторизация завершится ошибкой.
        _token = null;
        using HttpResponseMessage response = await _client.PostAsJsonAsync("api/Auth/SignIn", data, token);
        response.EnsureSuccessStatusCode();
        AuthResponse? result = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: token);
        if (result == null || string.IsNullOrWhiteSpace(result.Token))
        {
            throw new HttpRequestException("Authentication returned an empty token.");
        }

        _token = result.Token;
        return result;
    }

    public async Task AddAsync(Team team, CancellationToken token = default)
    {
        EnsureAuthorized();
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Team/Add");
        request.Content = JsonContent.Create(team);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        using HttpResponseMessage response = await _client.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TeamResponse> GetTeamsAsync(CancellationToken token = default)
    {
        EnsureAuthorized();
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/Team/GetTeams");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        using HttpResponseMessage response = await _client.SendAsync(request, token);
        response.EnsureSuccessStatusCode();
        TeamResponse? result = await response.Content.ReadFromJsonAsync<TeamResponse>(cancellationToken: token);
        if (result == null)
        {
            throw new HttpRequestException("Empty teams response.");
        }

        return result;
    }

    private void EnsureAuthorized()
    {
        if (string.IsNullOrWhiteSpace(_token))
        {
            throw new InvalidOperationException("Call AuthorizeAsync before protected requests.");
        }
    }
}

public sealed record CurrencyConversion(decimal Amount, string BaseCurrency, string QuoteCurrency, decimal ConvertedAmount, decimal Rate);
public sealed class CurrencyService
{
    private readonly HttpClient _client;
    public CurrencyService(HttpClient client)
    {
        _client = client;
    }

    public async Task<CurrencyConversion> ConvertAsync(decimal amount, string from, string to, CancellationToken token = default)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        from = NormalizeCode(from);
        to = NormalizeCode(to);
        token.ThrowIfCancellationRequested();
        if (from == to)
        {
            return new CurrencyConversion(amount, from, to, amount, 1);
        }

        string path = $"v2/rate/{from.ToLowerInvariant()}/{to.ToLowerInvariant()}";
        using HttpResponseMessage response = await _client.GetAsync(path, token);
        response.EnsureSuccessStatusCode();
        RateResponse? result = await response.Content.ReadFromJsonAsync<RateResponse>(cancellationToken: token);
        if (result == null || result.Rate <= 0)
        {
            throw new HttpRequestException("Currency service returned an invalid exchange rate.");
        }

        // Frankfurter возвращает курс; сумму вычисляем сами.
        decimal convertedAmount = decimal.Round(amount * result.Rate, 2);
        return new CurrencyConversion(amount, from, to, convertedAmount, result.Rate);
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
        {
            throw new ArgumentException("Expected a three-letter currency code.");
        }

        foreach (char letter in code)
        {
            if (!char.IsAsciiLetter(letter))
            {
                throw new ArgumentException("Currency code must contain only letters.");
            }
        }

        return code.ToUpperInvariant();
    }

    private sealed record RateResponse(decimal Rate);
}
