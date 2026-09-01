using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using SyncBar.Application.Features.Dining.Area.Create;
using SyncBar.Application.Features.Dining.Area.GetByBranchId;
using SyncBar.Application.Features.Dining.Area.GetById;
using SyncBar.Application.Features.Dining.Area.Update;
using SyncBar.Application.Features.Dining.Assignment.Create;
using SyncBar.Application.Features.Dining.Assignment.End;
using SyncBar.Application.Features.Dining.Assignment.GetActiveByDiningAreaId;
using SyncBar.Application.Features.Dining.Assignment.GetActiveByEmployeeId;
using SyncBar.Application.Features.Dining.Messages.Create;
using SyncBar.Application.Features.Dining.Messages.GetWaiterMessagesByBranch;
using SyncBar.Application.Features.Dining.Table.Create;
using SyncBar.Application.Features.Dining.Table.Deactivate;
using SyncBar.Application.Features.Dining.Table.GetByDiningAreaId;
using SyncBar.Application.Features.Dining.Table.GetById;
using SyncBar.Application.Features.Dining.Table.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers
{
    [Route("api/diningareas")]
    public sealed class DiningAreasController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
    {
        [HttpGet("branch/{branchId:long}")]
        public Task<IActionResult> GetByBranch(long branchId, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(GetByBranch), async () =>
            {
                var result = await Mediator.Send(new GetDiningAreasByBranchQuery(branchId), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [HttpGet("{id:long}")]
        public Task<IActionResult> GetById(long id, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(GetById), async () =>
            {
                var result = await Mediator.Send(new GetDiningAreaByIdQuery(id), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [Authorize(Roles = ManagerRoles)]
        [HttpPost]
        public Task<IActionResult> CreateArea([FromBody] CreateDiningAreaCommand command, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(CreateArea), async () =>
            {
                var result = await Mediator.Send(command, ct);
                return result.IsFailure
                    ? HandleFailure(result)
                    : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
            });

        [Authorize(Roles = ManagerRoles)]
        [HttpPut("{id:long}")]
        public Task<IActionResult> UpdateArea(long id, [FromBody] UpdateDiningAreaRequest request, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(UpdateArea), async () =>
            {
                var result = await Mediator.Send(new UpdateDiningAreaCommand(id, request.Name), ct);
                return result.IsFailure ? HandleFailure(result) : NoContent();
            });

        [HttpGet("{id:long}/tables")]
        public Task<IActionResult> GetTablesByArea(long id, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(GetTablesByArea), async () =>
            {
                var result = await Mediator.Send(new GetDiningAreaTablesByAreaIdQuery(id), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [Authorize(Roles = ManagerRoles)]
        [HttpPost("{id:long}/tables")]
        public Task<IActionResult> AssignTableToArea(long id, [FromBody] AssignTableRequest request, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(AssignTableToArea), async () =>
            {
                var result = await Mediator.Send(new CreateDiningAreaTableCommand(id, request.DiningTableId), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value); // Retorna o ID do vínculo
            });

        [Authorize(Roles = ManagerRoles)]
        [HttpPut("tables/{assignmentId:long}")]
        public Task<IActionResult> UpdateTableAssignment(long assignmentId, [FromBody] UpdateTableAssignmentRequest request, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(UpdateTableAssignment), async () =>
            {
                var result = await Mediator.Send(new UpdateDiningAreaTableCommand(assignmentId, request.DiningAreaId, request.DiningTableId), ct);
                return result.IsFailure ? HandleFailure(result) : NoContent();
            });

        [Authorize(Roles = ManagerRoles)]
        [HttpDelete("tables/{assignmentId:long}")]
        public Task<IActionResult> RemoveTableFromArea(long assignmentId, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(RemoveTableFromArea), async () =>
            {
                var result = await Mediator.Send(new DeactivateDiningAreaTableCommand(assignmentId), ct);
                return result.IsFailure ? HandleFailure(result) : NoContent();
            });

        [HttpGet("{id:long}/assignments/active")]
        public Task<IActionResult> GetActiveAssignmentsByArea(long id, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(GetActiveAssignmentsByArea), async () =>
            {
                var result = await Mediator.Send(new GetActiveAssignmentsByDiningAreaIdQuery(id), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [HttpGet("assignments/employee/{employeeId:long}/active")]
        public Task<IActionResult> GetActiveAssignmentsByEmployee(long employeeId, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(GetActiveAssignmentsByEmployee), async () =>
            {
                var result = await Mediator.Send(new GetActiveAssignmentsByEmployeeIdQuery(employeeId), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [Authorize(Roles = ManagerRoles)]
        [HttpPost("{id:long}/assignments")]
        public Task<IActionResult> StartAssignment(long id, [FromBody] StartAssignmentRequest request, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(StartAssignment), async () =>
            {
                var result = await Mediator.Send(new CreateDiningAreaAssignmentCommand(id, request.EmployeeId, request.StartAt), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [Authorize(Roles = ManagerRoles)]
        [HttpPut("assignments/{assignmentId:long}/end")]
        public Task<IActionResult> EndAssignment(long assignmentId, [FromBody] EndAssignmentRequest request, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(EndAssignment), async () =>
            {
                var result = await Mediator.Send(new EndDiningAreaAssignmentCommand(assignmentId, request.EndAt), ct);
                return result.IsFailure ? HandleFailure(result) : NoContent();
            });

        [HttpGet("messages/branch/{branchId:long}")]
        public Task<IActionResult> GetMessagesByBranch(long branchId, [FromQuery] long? diningAreaId, CancellationToken ct) =>
                ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(GetMessagesByBranch), async () =>
                {
                    var result = await Mediator.Send(new GetWaiterMessagesByBranchQuery(branchId, diningAreaId), ct);
                    return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
                });

        [HttpPost("messages")]
        public Task<IActionResult> SendMessage([FromBody] CreateWaiterMessageCommand command, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(DiningAreasController), nameof(SendMessage), async () =>
            {
                var result = await Mediator.Send(command, ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });
    }

    public sealed record UpdateDiningAreaRequest(string Name);
    public sealed record AssignTableRequest([property: JsonRequired] long DiningTableId);
    public sealed record UpdateTableAssignmentRequest([property: JsonRequired] long DiningAreaId, [property: JsonRequired] long DiningTableId);
    public sealed record StartAssignmentRequest([property: JsonRequired] long EmployeeId, [property: JsonRequired] DateTime StartAt);
    public sealed record EndAssignmentRequest([property: JsonRequired] DateTime EndAt);
}