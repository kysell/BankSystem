using BankSystem.App.Dto;
using FluentValidation;

namespace BankSystem.App.Validators;

public sealed class ClientRequestValidator : AbstractValidator<ClientRequest>
{
    public ClientRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PassportNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BirthDate).Must(IsAdult).WithMessage("Person must be at least 18 years old.");
    }

    internal static bool IsAdult(DateOnly date) => date > DateOnly.MinValue && date <= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-18);
}

public sealed class EmployeeRequestValidator : AbstractValidator<EmployeeRequest>
{
    public EmployeeRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PassportNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BirthDate).Must(ClientRequestValidator.IsAdult).WithMessage("Person must be at least 18 years old.");
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Salary).GreaterThanOrEqualTo(0);
    }
}
