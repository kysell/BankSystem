using BankSystem.App.Dto;
using BankSystem.App.Interfaces;
using BankSystem.App.Validators;
using BankSystem.Domain.Models;
using FluentValidation;

namespace BankSystem.App.Services;

public sealed class EmployeeService
{
    private readonly IEmployeeStorage _storage;
    private readonly IValidator<EmployeeRequest> _validator;
    public EmployeeService(IEmployeeStorage storage, IValidator<EmployeeRequest>? validator = null)
    {
        _storage = storage;
        _validator = validator ?? new EmployeeRequestValidator();
    }

    public async Task<EmployeeResponse?> GetResponseAsync(Guid id, CancellationToken token = default)
    {
        Employee? employee = await GetAsync(id, token);
        if (employee == null)
        {
            return null;
        }

        return employee.ToResponse();
    }

    public async Task<IReadOnlyList<EmployeeResponse>> SearchResponsesAsync(ClientFilter filter, CancellationToken token = default)
    {
        if (filter.BornFrom > filter.BornTo)
        {
            throw new ArgumentException("Birth date range is reversed.");
        }

        IReadOnlyList<Employee> employees = await _storage.GetAsync(employee =>
            (filter.FullName == null || (employee.FirstName + " " + employee.LastName).Contains(filter.FullName)) &&
            (filter.PhoneNumber == null || employee.PhoneNumber == filter.PhoneNumber) &&
            (filter.PassportNumber == null || employee.PassportNumber == filter.PassportNumber) &&
            (!filter.BornFrom.HasValue || employee.BirthDate >= filter.BornFrom.Value) &&
            (!filter.BornTo.HasValue || employee.BirthDate <= filter.BornTo.Value), token);
        var responses = new List<EmployeeResponse>();
        foreach (Employee employee in employees)
        {
            responses.Add(employee.ToResponse());
        }

        return responses;
    }

    public async Task<EmployeeResponse> CreateAsync(EmployeeRequest request, CancellationToken token = default)
    {
        await _validator.ValidateAndThrowAsync(request, token);
        Employee employee = request.ToModel();
        await AddAsync(employee, token);
        return employee.ToResponse();
    }

    public async Task UpdateAsync(Guid id, EmployeeRequest request, CancellationToken token = default)
    {
        await _validator.ValidateAndThrowAsync(request, token);
        Employee? previousEmployee = await GetAsync(id, token);
        if (previousEmployee == null)
        {
            throw new KeyNotFoundException("Employee not found.");
        }

        Employee updatedEmployee = request.ToModel(id);
        updatedEmployee.Bonus = previousEmployee.Bonus;
        updatedEmployee.Contract = previousEmployee.Contract;
        updatedEmployee.IsBankOwner = previousEmployee.IsBankOwner;
        await UpdateAsync(updatedEmployee, token);
    }

    public void Add(Employee employee)
    {
        ValidateEmployee(employee);
        _storage.Add(employee);
    }

    public Task AddAsync(Employee employee, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ValidateEmployee(employee);
        return _storage.AddAsync(employee, token);
    }

    public void Update(Employee employee)
    {
        ValidateEmployee(employee);
        _storage.Update(employee);
    }

    public Task UpdateAsync(Employee employee, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ValidateEmployee(employee);
        return _storage.UpdateAsync(employee, token);
    }

    public Task<Employee?> GetAsync(Guid id, CancellationToken token = default)
    {
        return _storage.GetByIdAsync(id, token);
    }

    public async Task DeleteAsync(Guid id, CancellationToken token = default)
    {
        Employee? employee = await _storage.GetByIdAsync(id, token);
        if (employee == null)
        {
            throw new KeyNotFoundException("Employee not found.");
        }

        await _storage.DeleteAsync(employee, token);
    }

    private static void ValidateEmployee(Employee employee)
    {
        PersonValidation.Validate(employee);
        if (employee.Age < 18)
        {
            throw new ArgumentException("Employee must be an adult.");
        }

        if (employee.Salary < 0)
        {
            throw new ArgumentException("Salary cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(employee.Position) || employee.Position.Length > 100)
        {
            throw new ArgumentException("Position is required (up to 100 characters).");
        }
    }
}
