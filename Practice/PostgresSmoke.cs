using BankSystem.App.Services;
using BankSystem.Data;
using BankSystem.Data.Storages;
using BankSystem.Domain.Models;
using ExportTool;
using Microsoft.EntityFrameworkCore;
using Npgsql;

internal static class PostgresSmoke
{
    public static async Task RunAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BankSystem") ?? throw new InvalidOperationException("Set ConnectionStrings__BankSystem.");
        var config = new NpgsqlConnectionStringBuilder(connectionString);
        if (Environment.GetEnvironmentVariable("BANKSYSTEM_ALLOW_TRAINING_DB") != "1" || config.Database?.StartsWith("banksystem_training_", StringComparison.Ordinal) != true)
        {
            throw new InvalidOperationException("Smoke writes require explicit BANKSYSTEM_ALLOW_TRAINING_DB=1 and a banksystem_training_* database.");
        }

        await using var db = new BankSystemDbContext(new DbContextOptionsBuilder<BankSystemDbContext>().UseNpgsql(connectionString).Options);
        using var storage = new ClientDbStorage(db);
        var service = new ClientService(storage);
        var client = new TestDataGenerator().CreateRandomClients(1, 123)[0];
        // Проверка создаёт собственные записи и удаляет только их.
        client = new Client
        {
            FirstName = client.FirstName,
            LastName = client.LastName,
            BirthDate = client.BirthDate,
            PhoneNumber = client.PhoneNumber,
            PassportNumber = client.PassportNumber
        };
        var employee = new TestDataGenerator().CreateRandomEmployees(1, 123)[0];
        employee = new Employee
        {
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            BirthDate = employee.BirthDate,
            PhoneNumber = employee.PhoneNumber,
            PassportNumber = employee.PassportNumber,
            Position = employee.Position,
            Salary = employee.Salary
        };
        var employees = new EmployeeDbStorage(db);
        var path = Path.Combine(Path.GetTempPath(), $"banksystem-pg-{Guid.NewGuid()}.csv");
        try
        {
            await service.AddClientAsync(client);
            await employees.AddAsync(employee);
            var account = client.Accounts.Single();
            account.Deposit(500);
            await storage.UpdateAccountAsync(client.Id, account);
            await service.WithdrawManyAsync(Enumerable.Repeat((client.Id, account.Id, 100m), 3));
            db.ChangeTracker.Clear();
            if ((await service.GetAsync(client.Id))!.Accounts.Single().Amount != 200)
            {
                throw new InvalidOperationException("Withdraw persistence failed.");
            }

            var second = new Account
            {
                Currency = new("EUR", .9m)
            };
            await service.AddAccountAsync(client.Id, second);
            db.ChangeTracker.Clear();
            if ((await service.GetAsync(client.Id))!.Accounts.Count != 2)
            {
                throw new InvalidOperationException("Account creation failed.");
            }

            await new RateUpdaterService(storage).ApplyMonthlyRateAsync(0); // нулевая ставка не меняет прочие учебные счета
            var exporter = new ExportService();
            await exporter.ExportClientsToCsvAsync([client], path);
            await service.DeleteAsync(client.Id);
            await exporter.ImportClientsIntoStorageAsync(path, service);
            db.ChangeTracker.Clear();
            if ((await service.GetAsync(client.Id))?.Accounts.Single().Currency.Code != "USD")
            {
                throw new InvalidOperationException("CSV database import failed.");
            }

            if (!(await service.SearchAsync(new ClientFilter(PhoneNumber: client.PhoneNumber))).Any(c => c.Id == client.Id))
            {
                throw new InvalidOperationException("Database filter failed.");
            }

            if (storage.Search(null, 1, 10).TotalCount < 1)
            {
                throw new InvalidOperationException("Pagination failed.");
            }

            Console.WriteLine("PostgreSQL smoke: client/employee CRUD, accounts, persisted withdrawals, CSV DB import and SQL filters passed.");
        }
        finally
        {
            db.ChangeTracker.Clear();
            if (await storage.GetByIdAsync(client.Id)is { } stored)
            {
                await storage.DeleteAsync(stored);
            }

            if (await employees.GetByIdAsync(employee.Id)is { } storedEmployee)
            {
                await employees.DeleteAsync(storedEmployee);
            }

            File.Delete(path);
        }
    }
}
