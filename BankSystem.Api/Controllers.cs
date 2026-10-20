using BankSystem.App.Dto;
using BankSystem.App.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankSystem.Api.Controllers;

[ApiController]
[Route("api/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly ClientService _service;
    public ClientsController(ClientService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientResponse>> Get(Guid id, CancellationToken token)
    {
        ClientResponse? client = await _service.GetResponseAsync(id, token);
        if (client == null)
        {
            return NotFound();
        }

        return Ok(client);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClientResponse>>> Search([FromQuery] string? name, [FromQuery] string? phone, [FromQuery] string? passport, [FromQuery] DateOnly? bornFrom, [FromQuery] DateOnly? bornTo, CancellationToken token) => Ok(await _service.SearchResponsesAsync(new ClientFilter(name, phone, passport, bornFrom, bornTo), token));
    [HttpPost]
    public async Task<ActionResult<ClientResponse>> Create(ClientRequest request, CancellationToken token)
    {
        var client = await _service.CreateAsync(request, token);
        return CreatedAtAction(nameof(Get), new { id = client.Id }, client);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ClientRequest request, CancellationToken token)
    {
        await _service.UpdateAsync(id, request, token);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        await _service.DeleteAsync(id, token);
        return NoContent();
    }
}

[ApiController]
[Route("api/employees")]
public sealed class EmployeesController : ControllerBase
{
    private readonly EmployeeService _service;
    public EmployeesController(EmployeeService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> Get(Guid id, CancellationToken token)
    {
        EmployeeResponse? employee = await _service.GetResponseAsync(id, token);
        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponse>>> GetAll([FromQuery] string? name, [FromQuery] string? phone, [FromQuery] string? passport, [FromQuery] DateOnly? bornFrom, [FromQuery] DateOnly? bornTo, CancellationToken token) => Ok(await _service.SearchResponsesAsync(new ClientFilter(name, phone, passport, bornFrom, bornTo), token));
    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> Create(EmployeeRequest request, CancellationToken token)
    {
        var employee = await _service.CreateAsync(request, token);
        return CreatedAtAction(nameof(Get), new { id = employee.Id }, employee);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, EmployeeRequest request, CancellationToken token)
    {
        await _service.UpdateAsync(id, request, token);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        await _service.DeleteAsync(id, token);
        return NoContent();
    }
}
