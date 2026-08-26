using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Employees.Create;
using SyncBar.Application.Features.Employees.CreateJobTitle;
using SyncBar.Application.Features.Employees.Dismiss;
using SyncBar.Application.Features.Employees.GetByBranch;
using SyncBar.Application.Features.Employees.GetJobTitles;
using SyncBar.Application.Features.Employees.SetCommission;
using SyncBar.Application.Features.Employees.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize]
public sealed class EmployeesController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public Task<IActionResult> GetByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(EmployeesController), nameof(GetByBranch), async () =>
        {
            var result = await Mediator.Send(new GetEmployeesByBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("jobtitles/company/{companyId:long}")]
    public Task<IActionResult> GetJobTitles(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(EmployeesController), nameof(GetJobTitles), async () =>
        {
            var result = await Mediator.Send(new GetJobTitlesQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Policy = "Feature:Equipe")]
    [HttpPost("jobtitles")]
    public Task<IActionResult> CreateJobTitle([FromBody] CreateJobTitleCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(EmployeesController), nameof(CreateJobTitle), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetJobTitles), new { companyId = command.CompanyId }, result.Value);
        });

    [Authorize(Policy = "Feature:Equipe")]
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateEmployeeCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(EmployeesController), nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetByBranch), new { branchId = command.BranchId }, result.Value);
        });

    [Authorize(Policy = "Feature:Equipe")]
    [HttpPut("{id:long}")]
    public Task<IActionResult> Update(long id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(EmployeesController), nameof(Update), async () =>
        {
            var result = await Mediator.Send(
                new UpdateEmployeeCommand(id, request.JobTitleId, request.Name, request.Email, request.Phone, request.Salary), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [Authorize(Policy = "Feature:Equipe")]
    [HttpPut("{id:long}/dismiss")]
    public Task<IActionResult> Dismiss(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(EmployeesController), nameof(Dismiss), async () =>
        {
            var result = await Mediator.Send(new DismissEmployeeCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("{id:long}/commission")]
    public Task<IActionResult> SetCommission(long id, [FromBody] SetCommissionRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(EmployeesController), nameof(SetCommission), async () =>
        {
            var result = await Mediator.Send(new SetCommissionCommand(id, request.CommissionPercent), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateEmployeeRequest(
    [property: JsonRequired] long JobTitleId, string Name, string? Email, string? Phone, decimal? Salary);
public sealed record SetCommissionRequest(decimal? CommissionPercent);