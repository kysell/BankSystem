using BankSystem.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankSystem.Data.EntityConfigurations;

public sealed class ClientEntityTypeConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("client");
        PersonConfiguration.Configure(builder);
        builder.HasMany(c => c.Accounts).WithOne().HasForeignKey("client_id").IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Accounts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class EmployeeEntityTypeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employee", t => t.HasCheckConstraint("ck_employee_salary", "salary >= 0"));
        PersonConfiguration.Configure(builder);
        builder.Property(x => x.Position).HasColumnName("position").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Salary).HasColumnName("salary").IsRequired();
        builder.Property(x => x.Contract).HasColumnName("contract").HasMaxLength(500);
        builder.Property(x => x.IsBankOwner).HasColumnName("is_bank_owner");
    }
}

public sealed class AccountEntityTypeConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("account", t => t.HasCheckConstraint("ck_account_amount", "amount >= 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsConcurrencyToken();
        builder.HasOne(x => x.Currency).WithMany().HasForeignKey("currency_code").IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.Property<string>("currency_code").HasMaxLength(3);
    }
}

public sealed class CurrencyEntityTypeConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currency", t => t.HasCheckConstraint("ck_currency_rate", "exchange_rate > 0"));
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(3).ValueGeneratedNever();
        builder.Property(x => x.ExchangeRate).HasColumnName("exchange_rate").HasPrecision(18, 6);
    }
}

internal static class PersonConfiguration
{
    internal static void Configure<T>(EntityTypeBuilder<T> builder)
        where T : Person
    {
        builder.Ignore(x => x.Age);
        builder.Ignore(x => x.FullName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.BirthDate).HasColumnName("birth_date").IsRequired();
        builder.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(30).IsRequired();
        builder.Property(x => x.PassportNumber).HasColumnName("passport_number").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Bonus).HasColumnName("bonus").HasPrecision(18, 2);
    }
}
