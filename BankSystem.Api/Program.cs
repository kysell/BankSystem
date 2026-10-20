using BankSystem.App.Dto;
using BankSystem.App.Validators;
using FluentValidation;
using BankSystem.App.Interfaces;
using BankSystem.App.Services;
using BankSystem.Data.Storages;
using BankSystem.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
var connectionString = builder.Configuration.GetConnectionString("BankSystem");
if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddSingleton<IClientStorage, ClientStorage>();
    builder.Services.AddSingleton<IEmployeeStorage, EmployeeStorage>();
}
else
{
    builder.Services.AddDbContext<BankSystemDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddScoped<IClientStorage, ClientDbStorage>();
    builder.Services.AddScoped<IEmployeeStorage, EmployeeDbStorage>();
}

builder.Services.AddScoped<IValidator<ClientRequest>, ClientRequestValidator>();
builder.Services.AddScoped<IValidator<EmployeeRequest>, EmployeeRequestValidator>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddHttpClient<CurrencyService>(client =>
{
    client.BaseAddress = new Uri("https://api.frankfurter.dev/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
var app = builder.Build();
app.UseExceptionHandler();
app.MapOpenApi();
// Swagger UI поставляется NuGet-пакетом и не требует доступа к CDN.
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "BankSystem v1");
});
app.MapControllers();
app.Run();
public partial class Program;
public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken token)
    {
        int status;
        if (exception is UnderageClientException || exception is ArgumentException || exception is ValidationException)
        {
            status = StatusCodes.Status400BadRequest;
        }
        else if (exception is KeyNotFoundException)
        {
            status = StatusCodes.Status404NotFound;
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            status = StatusCodes.Status409Conflict;
        }
        else
        {
            status = StatusCodes.Status500InternalServerError;
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = status == 500 ? "Unexpected server error." : exception.Message }, token);
        return true;
    }
}
