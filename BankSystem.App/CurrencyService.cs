using System.Net.Http.Json;

namespace BankSystem.App.Services;

public sealed class CurrencyService
{
    private readonly HttpClient _client;
    public CurrencyService(HttpClient client)
    {
        _client = client;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string from, string to, CancellationToken token = default)
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
            return amount;
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
        return convertedAmount;
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
