using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Create;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Delete;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Exists;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetByAsaasCustomerId;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetById;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Update;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Route("api/asaas/customers")]
public sealed class AsaasCustomerController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId.AsaasIntegrationCustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasCustomerController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetAsaasCustomerByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}")]
    [ProducesResponseType(typeof(IReadOnlyList<SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId.AsaasIntegrationCustomerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAllByCompany(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasCustomerController), nameof(GetAllByCompany), async () =>
        {
            var result = await Mediator.Send(new GetAllAsaasCustomersByCompanyIdQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}/customer/{customerId:long}")]
    [ProducesResponseType(typeof(SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId.AsaasIntegrationCustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByCustomerAndCompany(long companyId, long customerId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasCustomerController), nameof(GetByCustomerAndCompany), async () =>
        {
            var result = await Mediator.Send(new GetByCustomerIdAndCompanyIdQuery(customerId, companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("asaas-id/{asaasCustomerId}")]
    [ProducesResponseType(typeof(SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId.AsaasIntegrationCustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetByAsaasCustomerId(string asaasCustomerId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasCustomerController), nameof(GetByAsaasCustomerId), async () =>
        {
            var result = await Mediator.Send(new GetByAsaasCustomerIdQuery(asaasCustomerId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Exists(
        [FromQuery] long customerId,
        [FromQuery] long companyId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasCustomerController), nameof(Exists), async () =>
        {
            var result = await Mediator.Send(new ExistsAsaasCustomerQuery(customerId, companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    [ProducesResponseType(typeof(long), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create(
        [FromBody] CreateAsaasIntegrationCustomerCommand command,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasCustomerController), nameof(Create), async () =>
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
        [FromBody] UpdateCustomerRequest request,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasCustomerController), nameof(Update), async () =>
        {
            var command = new UpdateAsaasIntegrationCustomerCommand(id, request.NewAsaasCustomerId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("company/{companyId:long}/customer/{customerId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(
        long companyId,
        long customerId,
        CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AsaasCustomerController), nameof(Delete), async () =>
        {
            var command = new DeleteAsaasCustomerCommand(customerId, companyId);
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateCustomerRequest(string NewAsaasCustomerId);