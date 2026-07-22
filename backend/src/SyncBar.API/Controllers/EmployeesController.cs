using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Employees.Create;
using SyncBar.Application.Features.Employees.Dismiss;
using SyncBar.Application.Features.Employees.GetByBranch;
using SyncBar.Application.Features.Employees.GetJobTitles;
using SyncBar.Application.Features.Employees.SetCommission;
using SyncBar.Application.Features.Employees.Update;

namespace SyncBar.API.Controllers;

[Authorize]
public sealed class EmployeesController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public async Task<IActionResult> GetByBranch(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetEmployeesByBranchQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpGet("jobtitles/company/{companyId:long}")]
    public async Task<IActionResult> GetJobTitles(long companyId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetJobTitlesQuery(companyId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [Authorize(Policy = "Feature:Equipe")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure
            ? HandleFailure(result)
            : CreatedAtAction(nameof(GetByBranch), new { branchId = command.BranchId }, result.Value);
    }

    [Authorize(Policy = "Feature:Equipe")]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new UpdateEmployeeCommand(id, request.JobTitleId, request.Name, request.Email, request.Phone, request.Salary), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [Authorize(Policy = "Feature:Equipe")]
    [HttpPut("{id:long}/dismiss")]
    public async Task<IActionResult> Dismiss(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DismissEmployeeCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    // Prerrogativa do gerente — define o % de comissão sobre vendas do garçom/vendedor.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("{id:long}/commission")]
    public async Task<IActionResult> SetCommission(long id, [FromBody] SetCommissionRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SetCommissionCommand(id, request.CommissionPercent), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}

public sealed record UpdateEmployeeRequest(long JobTitleId, string Name, string? Email, string? Phone, decimal? Salary);
public sealed record SetCommissionRequest(decimal? CommissionPercent);
