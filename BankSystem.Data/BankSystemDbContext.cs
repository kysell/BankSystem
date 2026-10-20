using BankSystem.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BankSystem.Data;

public sealed class BankSystemDbContext : DbContext
{
    // Настройки подключения приходят из Program.cs или из фабрики миграций.
    public BankSystemDbContext(DbContextOptions<BankSystemDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Currency> Currencies => Set<Currency>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Здесь явно видно, какие таблицы и связи настраивает приложение.
        modelBuilder.ApplyConfiguration(new EntityConfigurations.ClientEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new EntityConfigurations.EmployeeEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new EntityConfigurations.AccountEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new EntityConfigurations.CurrencyEntityTypeConfiguration());
    }
}

public sealed class BankSystemDbContextFactory : IDesignTimeDbContextFactory<BankSystemDbContext>
{
    public BankSystemDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BankSystem") ?? "Host=localhost;Port=5432;Database=banksystem_training_example;Username=postgres";
        return new BankSystemDbContext(new DbContextOptionsBuilder<BankSystemDbContext>().UseNpgsql(connectionString).Options);
    }
}
