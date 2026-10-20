using System.ComponentModel.DataAnnotations;
using BankSystem.Domain.Models;

namespace BankSystem.App.Dto;

public sealed record ClientRequest(
    [Required, StringLength(100)] string FirstName,
    [Required, StringLength(100)] string LastName,
    DateOnly BirthDate,
    [Required, StringLength(30)] string PhoneNumber,
    [Required, StringLength(50)] string PassportNumber);

public sealed record EmployeeRequest(
    [Required, StringLength(100)] string FirstName,
    [Required, StringLength(100)] string LastName,
    DateOnly BirthDate,
    [Required, StringLength(30)] string PhoneNumber,
    [Required, StringLength(50)] string PassportNumber,
    [Required, StringLength(100)] string Position,
    [Range(0, int.MaxValue)] int Salary);

public sealed record AccountResponse(Guid Id, string Currency, decimal Amount);

public sealed record ClientResponse(
    Guid Id, string FirstName, string LastName, DateOnly BirthDate,
    string PhoneNumber, string? PassportNumber, IReadOnlyList<AccountResponse> Accounts);

public sealed record EmployeeResponse(
    Guid Id, string FirstName, string LastName, DateOnly BirthDate,
    string PhoneNumber, string? PassportNumber, string Position, int Salary);

// Явно копируем нужные поля между запросом, моделью и ответом.
public static class DtoMapping
{
    public static Client ToModel(this ClientRequest request, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        FirstName = request.FirstName,
        LastName = request.LastName,
        BirthDate = request.BirthDate,
        PhoneNumber = request.PhoneNumber,
        PassportNumber = request.PassportNumber
    };
    public static Employee ToModel(this EmployeeRequest request, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        FirstName = request.FirstName,
        LastName = request.LastName,
        BirthDate = request.BirthDate,
        PhoneNumber = request.PhoneNumber,
        PassportNumber = request.PassportNumber,
        Position = request.Position,
        Salary = request.Salary
    };
    public static ClientResponse ToResponse(this Client client)
    {
        var accounts = new List<AccountResponse>();
        foreach (Account account in client.Accounts)
        {
            accounts.Add(new AccountResponse(account.Id, account.Currency.Code, account.Amount));
        }

        return new ClientResponse(client.Id, client.FirstName, client.LastName, client.BirthDate, client.PhoneNumber, client.PassportNumber, accounts);
    }

    public static EmployeeResponse ToResponse(this Employee employee) => new(employee.Id, employee.FirstName, employee.LastName, employee.BirthDate, employee.PhoneNumber, employee.PassportNumber, employee.Position, employee.Salary);
}
