using BankSystem.Domain.Models;

namespace BankSystem.App.Services;

public sealed class UnderageClientException : Exception
{
    public UnderageClientException(string message) : base(message)
    {
    }
}

public sealed record ClientFilter(string? FullName = null, string? PhoneNumber = null, string? PassportNumber = null, DateOnly? BornFrom = null, DateOnly? BornTo = null);
public sealed class BankService
{
    private readonly HashSet<Guid> _blackList = new HashSet<Guid>();
    public void UpdateEmployeeContract(Employee employee)
    {
        employee.Contract = $"{employee.FullName}; {employee.Position}; salary {employee.Salary}";
    }

    public void UpdateCurrency(CurrencyValue currency, string code, decimal rate)
    {
        // Меняется только копия структуры внутри метода.
        currency.Code = code;
        currency.ExchangeRate = rate;
    }

    public void UpdateCurrency(ref CurrencyValue currency, string code, decimal rate)
    {
        currency = new CurrencyValue(code, rate);
    }

    public void UpdateCurrency(ref Currency currency, string code, decimal rate)
    {
        currency = new Currency(code, rate);
    }

    public int CalculateOwnersSalary(decimal profit, decimal expenses, IEnumerable<Employee> employees)
    {
        int ownerCount = employees.Count(employee => employee.IsBankOwner);
        if (ownerCount == 0)
        {
            throw new InvalidOperationException("No bank owners.");
        }

        decimal salary = (profit - expenses) / ownerCount;
        return (int)salary;
    }

    public Employee ConvertClientToEmployee(Client client)
    {
        // Client и Employee — разные наследники Person. Приведение типа не заменяет создание сотрудника.
        return new Employee
        {
            Id = client.Id,
            Bonus = client.Bonus,
            FirstName = client.FirstName,
            LastName = client.LastName,
            BirthDate = client.BirthDate,
            PhoneNumber = client.PhoneNumber,
            PassportNumber = client.PassportNumber,
            Position = "Former client"
        };
    }

    public void AddBonus<T>(T person, decimal amount)
        where T : Person
    {
        person.Bonus += amount;
    }

    public void AddToBlackList<T>(T person)
        where T : Person
    {
        _blackList.Add(person.Id);
    }

    public bool IsPersonInBlackList<T>(T person)
        where T : Person
    {
        return _blackList.Contains(person.Id);
    }
}
