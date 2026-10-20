using BankSystem.Domain.Models;

namespace BankSystem.App.Services;

internal static class PersonValidation
{
    public static void Validate(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);
        if (person.BirthDate == DateOnly.MinValue)
        {
            throw new ArgumentException("Birth date is required.");
        }

        if (person.Id == Guid.Empty)
        {
            throw new ArgumentException("Person ID is required.");
        }
        if (string.IsNullOrWhiteSpace(person.FirstName) || person.FirstName.Length > 100)
        {
            throw new ArgumentException("First name is required (up to 100 characters).");
        }
        if (string.IsNullOrWhiteSpace(person.LastName) || person.LastName.Length > 100)
        {
            throw new ArgumentException("Last name is required (up to 100 characters).");
        }

        if (string.IsNullOrWhiteSpace(person.PassportNumber) || person.PassportNumber.Length > 50)
        {
            throw new ArgumentException("Passport is required (up to 50 characters).");
        }

        if (string.IsNullOrWhiteSpace(person.PhoneNumber) || person.PhoneNumber.Length > 30)
        {
            throw new ArgumentException("Phone is required (up to 30 characters).");
        }
    }
}
