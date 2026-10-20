using System.Text.Json.Serialization;

namespace BankSystem.Domain.Models;

public abstract class Person
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PassportNumber { get; set; }
    public decimal Bonus { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();

    public int Age
    {
        get
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            int age = today.Year - BirthDate.Year;
            DateOnly birthdayThisYear = BirthDate.AddYears(age);
            if (today < birthdayThisYear)
            {
                age--;
            }

            return age;
        }
    }
}

public sealed class Employee : Person, IEquatable<Employee>
{
    public string Position { get; set; } = string.Empty;
    public int Salary { get; set; }
    public string Contract { get; set; } = string.Empty;
    public bool IsBankOwner { get; set; }

    public bool Equals(Employee? other) => other != null && Id == other.Id;
    public override bool Equals(object? obj) => Equals(obj as Employee);
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed class Client : Person, IEquatable<Client>
{
    private readonly List<Account> _accounts = new List<Account>();
    // При чтении JSON нужно восстановить и вложенные счета.
    [JsonInclude]
    public IReadOnlyList<Account> Accounts
    {
        get => _accounts.AsReadOnly();
        private set
        {
            _accounts.Clear();
            foreach (Account account in value)
            {
                AddAccount(account);
            }
        }
    }

    public void AddAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        foreach (Account existingAccount in _accounts)
        {
            if (existingAccount.Id == account.Id)
            {
                throw new InvalidOperationException("Account already exists.");
            }
        }

        _accounts.Add(account);
    }

    public bool RemoveAccount(Guid accountId)
    {
        Account? account = _accounts.Find(item => item.Id == accountId);
        if (account == null)
        {
            return false;
        }

        return _accounts.Remove(account);
    }

    public bool Equals(Client? other) => other != null && Id == other.Id;
    public override bool Equals(object? obj) => Equals(obj as Client);
    public override int GetHashCode() => Id.GetHashCode();
}

// Структура осталась для упражнения с копированием значений.
public struct CurrencyValue
{
    public string Code { get; set; }
    public decimal ExchangeRate { get; set; }

    public CurrencyValue(string code, decimal exchangeRate)
    {
        Code = code;
        ExchangeRate = exchangeRate;
    }

    public override readonly string ToString() => $"{Code}: {ExchangeRate}";
}

// Класс используется как справочник валют в базе данных.
public sealed class Currency
{
    public string Code { get; set; }
    public decimal ExchangeRate { get; set; }

    public Currency(string code, decimal exchangeRate)
    {
        Code = code;
        ExchangeRate = exchangeRate;
    }

    public override string ToString() => $"{Code}: {ExchangeRate}";
}

public sealed class Account
{
    private readonly object _sync = new object ();
    private decimal _amount;
    public Guid Id { get; init; } = Guid.NewGuid();
    public Currency Currency { get; set; } = new Currency("USD", 1m);

    [JsonInclude]
    public decimal Amount
    {
        get
        {
            lock (_sync)
            {
                return _amount;
            }
        }

        private set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            lock (_sync)
            {
                _amount = value;
            }
        }
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        // Проверка и изменение баланса выполняются без вмешательства другого потока.
        lock (_sync)
        {
            _amount += amount;
        }
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        lock (_sync)
        {
            if (_amount < amount)
            {
                throw new InvalidOperationException("Insufficient funds.");
            }

            _amount -= amount;
        }
    }

    public void SetAmount(decimal amount)
    {
        Amount = amount;
    }
}

public sealed class Circle : IComparable<Circle>
{
    public double Radius { get; }
    public double Area => Math.PI * Radius * Radius;

    public Circle(double radius)
    {
        if (!double.IsFinite(radius) || radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        Radius = radius;
    }

    public int CompareTo(Circle? other)
    {
        if (other == null)
        {
            return 1;
        }

        return Area.CompareTo(other.Area);
    }
}

public sealed class CircleAreaDescendingComparer : IComparer<Circle>
{
    public int Compare(Circle? first, Circle? second)
    {
        return Comparer<Circle>.Default.Compare(second, first);
    }
}

public interface IAnimal
{
    string Speak();
}

public abstract class Animal : IAnimal
{
    public string Name { get; }

    protected Animal(string name)
    {
        Name = name;
    }

    public abstract string Speak();
}

public sealed class Dog : Animal
{
    public Dog(string name) : base(name)
    {
    }

    public override string Speak() => "Woof";
}

public sealed class Cat : Animal
{
    public Cat(string name) : base(name)
    {
    }

    public override string Speak() => "Meow";
}
