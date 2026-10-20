using System.Globalization;
using System.Text;
using System.Text.Json;
using BankSystem.App.Services;
using BankSystem.Domain.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace ExportTool;

public sealed class ExportService
{
    private static readonly string[] Headers =
    {
        "Id",
        "FirstName",
        "LastName",
        "BirthDate",
        "PhoneNumber",
        "PassportNumber"
    };
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };
    public async Task ExportClientsToCsvAsync(IEnumerable<Client> clients, string path, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(clients);
        token.ThrowIfCancellationRequested();
        EnsureDirectory(path);
        await using var writer = new StreamWriter(File.Create(path), new UTF8Encoding(false));
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        foreach (string header in Headers)
        {
            csv.WriteField(header);
        }

        await csv.NextRecordAsync();
        foreach (Client client in clients)
        {
            token.ThrowIfCancellationRequested();
            // CsvHelper сам экранирует запятые, кавычки и переносы строк.
            csv.WriteField(client.Id);
            csv.WriteField(client.FirstName);
            csv.WriteField(client.LastName);
            csv.WriteField(client.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            csv.WriteField(client.PhoneNumber);
            csv.WriteField(client.PassportNumber ?? string.Empty);
            await csv.NextRecordAsync();
        }
    }

    public async Task<IReadOnlyList<Client>> ImportClientsFromCsvAsync(string path, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using var reader = new StreamReader(path, Encoding.UTF8);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = false
        };
        using var csv = new CsvReader(reader, configuration);
        var clients = new List<Client>();
        var ids = new HashSet<Guid>();
        try
        {
            if (!await csv.ReadAsync())
            {
                throw new FormatException("CSV header is missing.");
            }

            csv.ReadHeader();
            if (csv.HeaderRecord == null || !csv.HeaderRecord.SequenceEqual(Headers))
            {
                throw new FormatException("Invalid CSV header.");
            }

            while (await csv.ReadAsync())
            {
                token.ThrowIfCancellationRequested();
                if (csv.Parser.Count != Headers.Length)
                {
                    throw new FormatException("Expected six CSV columns.");
                }

                Guid id = csv.GetField<Guid>("Id");
                if (!ids.Add(id))
                {
                    throw new FormatException("Duplicate client ID in CSV.");
                }

                var client = new Client
                {
                    Id = id,
                    FirstName = csv.GetField("FirstName") ?? string.Empty,
                    LastName = csv.GetField("LastName") ?? string.Empty,
                    BirthDate = DateOnly.ParseExact(csv.GetField("BirthDate")!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    PhoneNumber = csv.GetField("PhoneNumber") ?? string.Empty,
                    PassportNumber = csv.GetField("PassportNumber")
                };
                clients.Add(client);
            }
        }
        catch (CsvHelperException exception)
        {
            throw new FormatException("Invalid CSV data.", exception);
        }

        return clients;
    }

    public async Task ImportClientsIntoStorageAsync(string path, ClientService service, CancellationToken token = default)
    {
        IReadOnlyList<Client> clients = await ImportClientsFromCsvAsync(path, token);
        foreach (Client client in clients)
        {
            await service.AddClientAsync(client, token);
        }
    }

    public async Task ExportJsonAsync<T>(T value, string path, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        EnsureDirectory(path);
        await using var file = File.Create(path);
        await JsonSerializer.SerializeAsync(file, value, JsonOptions, token);
    }

    public async Task<T?> ImportJsonAsync<T>(string path, CancellationToken token = default)
    {
        await using var file = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(file, JsonOptions, token);
    }

    private static void EnsureDirectory(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
    }
}
