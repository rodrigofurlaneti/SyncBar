using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.CustomerAddresses.Create;
using SyncBar.Application.Features.CustomerAddresses.GetByBranchId;
using SyncBar.Application.Features.CustomerAddresses.GetByCompanyId;
using SyncBar.Application.Features.CustomerAddresses.GetByCustomerId;
using SyncBar.Application.Features.CustomerAddresses.GetById;
using SyncBar.Application.Features.CustomerAddresses.RegisterOrder;
using SyncBar.Application.Features.CustomerAddresses.Remove;
using SyncBar.Application.Features.CustomerAddresses.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers
{
    [Route("api/customeraddresses")]
    public sealed class CustomerAddressesController(IMediator mediator) : ApiController(mediator)
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerAddressCommand command, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(nameof(CustomerAddressesController), nameof(Create), async () =>
            {
                var result = await Mediator.Send(command, cancellationToken);
                return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
            });
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateCustomerAddressCommand command, CancellationToken cancellationToken)
        {
            if (id != command.Id)
                return BadRequest(new { message = "The route ID does not match the command ID." });

            return await ExecuteWithLogAsync(nameof(CustomerAddressesController), nameof(Update), async () =>
            {
                var result = await Mediator.Send(command, cancellationToken);
                return result.IsSuccess ? NoContent() : HandleFailure(result);
            });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Remove(long id, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(nameof(CustomerAddressesController), nameof(Remove), async () =>
            {
                var command = new RemoveCustomerAddressCommand(id);
                var result = await Mediator.Send(command, cancellationToken);
                return result.IsSuccess ? NoContent() : HandleFailure(result);
            });
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(nameof(CustomerAddressesController), nameof(GetById), async () =>
            {
                var query = new GetCustomerAddressByIdQuery(id);
                var result = await Mediator.Send(query, cancellationToken);
                return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
            });
        }

        [HttpGet("customer/{customerId:long}")]
        public async Task<IActionResult> GetByCustomerId(long customerId, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(nameof(CustomerAddressesController), nameof(GetByCustomerId), async () =>
            {
                var query = new GetCustomerAddressesByCustomerIdQuery(customerId);
                var result = await Mediator.Send(query, cancellationToken);
                return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
            });
        }

        [HttpGet("company/{companyId:long}")]
        public async Task<IActionResult> GetByCompanyId(long companyId, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(nameof(CustomerAddressesController), nameof(GetByCompanyId), async () =>
            {
                var query = new GetCustomerAddressesByCompanyIdQuery(companyId);
                var result = await Mediator.Send(query, cancellationToken);
                return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
            });
        }

        [HttpGet("branch/{branchId:long}")]
        public async Task<IActionResult> GetByBranchId(long branchId, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(nameof(CustomerAddressesController), nameof(GetByBranchId), async () =>
            {
                var query = new GetCustomerAddressesByBranchIdQuery(branchId);
                var result = await Mediator.Send(query, cancellationToken);
                return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
            });
        }

        [HttpPatch("{id:long}/register-order")]
        public async Task<IActionResult> RegisterOrder(long id, [FromBody] RegisterCustomerAddressOrderRequest request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(nameof(CustomerAddressesController), nameof(RegisterOrder), async () =>
            {
                var command = new RegisterCustomerAddressOrderCommand(id, request.OrderId);
                var result = await Mediator.Send(command, cancellationToken);
                return result.IsSuccess ? NoContent() : HandleFailure(result);
            });
        }
    }
    public sealed record RegisterCustomerAddressOrderRequest(long OrderId);
}