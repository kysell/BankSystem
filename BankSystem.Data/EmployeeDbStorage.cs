using System.Linq.Expressions;
using BankSystem.App.Interfaces;
using BankSystem.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BankSystem.Data.Storages;

public sealed class EmployeeDbStorage : IEmployeeStorage
{
    private readonly BankSystemDbContext _context;
    public EmployeeDbStorage(BankSystemDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<Employee> Get(Expression<Func<Employee, bool>>? filter = null)
    {
        return CreateQuery(filter).AsNoTracking().ToList();
    }

    public async Task<IReadOnlyList<Employee>> GetAsync(Expression<Func<Employee, bool>>? filter = null, CancellationToken token = default)
    {
        return await CreateQuery(filter).AsNoTracking().ToListAsync(token);
    }

    public Employee? GetById(Guid id)
    {
        return _context.Employees.SingleOrDefault(employee => employee.Id == id);
    }

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        return _context.Employees.SingleOrDefaultAsync(employee => employee.Id == id, token);
    }

    public void Add(Employee employee)
    {
        _context.Employees.Add(employee);
        _context.SaveChanges();
    }

    public async Task AddAsync(Employee employee, CancellationToken token = default)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(token);
    }

    public void Update(Employee employee)
    {
        Employee? savedEmployee = GetById(employee.Id);
        if (savedEmployee == null)
        {
            throw new KeyNotFoundException("Employee not found.");
        }

        _context.Entry(savedEmployee).CurrentValues.SetValues(employee);
        _context.SaveChanges();
    }

    public async Task UpdateAsync(Employee employee, CancellationToken token = default)
    {
        Employee? savedEmployee = await GetByIdAsync(employee.Id, token);
        if (savedEmployee == null)
        {
            throw new KeyNotFoundException("Employee not found.");
        }

        _context.Entry(savedEmployee).CurrentValues.SetValues(employee);
        await _context.SaveChangesAsync(token);
    }

    public void Delete(Employee employee)
    {
        Employee? savedEmployee = GetById(employee.Id);
        if (savedEmployee == null)
        {
            throw new KeyNotFoundException("Employee not found.");
        }

        _context.Employees.Remove(savedEmployee);
        _context.SaveChanges();
    }

    public async Task DeleteAsync(Employee employee, CancellationToken token = default)
    {
        Employee? savedEmployee = await GetByIdAsync(employee.Id, token);
        if (savedEmployee == null)
        {
            throw new KeyNotFoundException("Employee not found.");
        }

        _context.Employees.Remove(savedEmployee);
        await _context.SaveChangesAsync(token);
    }

    private IQueryable<Employee> CreateQuery(Expression<Func<Employee, bool>>? filter)
    {
        IQueryable<Employee> query = _context.Employees;
        if (filter != null)
        {
            query = query.Where(filter);
        }

        return query;
    }
}
