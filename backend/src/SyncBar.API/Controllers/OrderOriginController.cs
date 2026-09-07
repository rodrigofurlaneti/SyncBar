using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.OrderOrigin.Create;
using SyncBar.Application.Features.OrderOrigin.ExistsByName;
using SyncBar.Application.Features.OrderOrigin.GetAll;
using SyncBar.Application.Features.OrderOrigin.GetByCompanyAndBranch;
using SyncBar.Application.Features.OrderOrigin.GetById;
using SyncBar.Application.Features.OrderOrigin.GetFiltered;
using SyncBar.Application.Features.OrderOrigin.Remove;
using SyncBar.Application.Features.OrderOrigin.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers
{
    [Route("api/order-origins")]
    public sealed class OrderOriginController(
        IMediator mediator,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork) : ApiController(mediator)
    {
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(OrderOriginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetById(long id, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrderOriginController), nameof(GetById), async () =>
            {
                var result = await Mediator.Send(new GetOrderOriginByIdQuery(id), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyCollection<OrderOriginResponse>), StatusCodes.Status200OK)]
        public Task<IActionResult> GetAll(CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrderOriginController), nameof(GetAll), async () =>
            {
                var result = await Mediator.Send(new GetAllOrderOriginsQuery(), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [HttpGet("company/{companyId:long}/branch/{branchId:long}")]
        [ProducesResponseType(typeof(IReadOnlyCollection<OrderOriginResponse>), StatusCodes.Status200OK)]
        public Task<IActionResult> GetByCompanyAndBranch(long companyId, long branchId, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrderOriginController), nameof(GetByCompanyAndBranch), async () =>
            {
                var result = await Mediator.Send(new GetOrderOriginsByCompanyAndBranchQuery(companyId, branchId), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [HttpGet("filtered")]
        [ProducesResponseType(typeof(IReadOnlyCollection<OrderOriginResponse>), StatusCodes.Status200OK)]
        public Task<IActionResult> GetFiltered(
            [FromQuery] long? companyId,
            [FromQuery] long? branchId,
            [FromQuery] string? searchTerm,
            [FromQuery] bool? isActive,
            CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrderOriginController), nameof(GetFiltered), async () =>
            {
                var result = await Mediator.Send(new GetFilteredOrderOriginsQuery(companyId, branchId, searchTerm, isActive), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [HttpGet("exists")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public Task<IActionResult> ExistsByName(
            [FromQuery] long? companyId,
            [FromQuery] long? branchId,
            [FromQuery] string name,
            [FromQuery] long? excludeId,
            CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrderOriginController), nameof(ExistsByName), async () =>
            {
                var result = await Mediator.Send(new ExistsOrderOriginByNameQuery(companyId, branchId, name, excludeId), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [HttpPost]
        [ProducesResponseType(typeof(long), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public Task<IActionResult> Create(
            [FromBody] CreateOrderOriginCommand command,
            CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrderOriginController), nameof(Create), async () =>
            {
                var result = await Mediator.Send(command, ct);
                return result.IsFailure
                    ? HandleFailure(result)
                    : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
            });

        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Update(
            long id,
            [FromBody] UpdateOrderOriginRequest request,
            CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrderOriginController), nameof(Update), async () =>
            {
                var command = new UpdateOrderOriginCommand(id, request.CompanyId, request.BranchId, request.Name, request.IsActive);
                var result = await Mediator.Send(command, ct);
                return result.IsFailure ? HandleFailure(result) : NoContent();
            });

        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Delete(long id, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrderOriginController), nameof(Delete), async () =>
            {
                var result = await Mediator.Send(new RemoveOrderOriginCommand(id), ct);
                return result.IsFailure ? HandleFailure(result) : NoContent();
            });
    }

    public sealed record UpdateOrderOriginRequest(
        long? CompanyId,
        long? BranchId,
        string Name,
        bool IsActive);
}
