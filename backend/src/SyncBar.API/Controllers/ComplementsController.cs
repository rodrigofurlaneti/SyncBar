using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Catalog.Complements.AddComplement;
using SyncBar.Application.Features.Catalog.Complements.CreateComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.CreateComplementItem;
using SyncBar.Application.Features.Catalog.Complements.DeactivateComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.DeactivateComplementItem;
using SyncBar.Application.Features.Catalog.Complements.GetComplementGroups;
using SyncBar.Application.Features.Catalog.Complements.GetComplementItems;
using SyncBar.Application.Features.Catalog.Complements.GetProductComplementGroups;
using SyncBar.Application.Features.Catalog.Complements.LinkProductComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.RemoveComplement;
using SyncBar.Application.Features.Catalog.Complements.UnlinkProductComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementGroup;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementItem;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementPrice;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

// Fase 6a: cadastro de complementos (ComplementItem/ComplementGroup/Complement) e vínculo com
// produtos (ProductComplementGroup) — mesma policy de ProductsController, é parte do cardápio.
[Authorize(Policy = "Feature:Cardapio")]
[Route("api/complements")]
public sealed class ComplementsController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    // --- ComplementItem (cadastro leve) ---

    [HttpGet("items/company/{companyId:long}")]
    public Task<IActionResult> GetItems(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(GetItems), async () =>
        {
            var result = await Mediator.Send(new GetComplementItemsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("items")]
    public Task<IActionResult> CreateItem([FromBody] CreateComplementItemCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(CreateItem), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("items/{id:long}")]
    public Task<IActionResult> UpdateItem(long id, [FromBody] UpdateComplementItemRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(UpdateItem), async () =>
        {
            var result = await Mediator.Send(new UpdateComplementItemCommand(id, request.Name), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("items/{id:long}/deactivate")]
    public Task<IActionResult> DeactivateItem(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(DeactivateItem), async () =>
        {
            var result = await Mediator.Send(new DeactivateComplementItemCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // --- ComplementGroup (agrupador, ex.: "Escolha uma bebida") ---

    [HttpGet("groups/company/{companyId:long}")]
    public Task<IActionResult> GetGroups(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(GetGroups), async () =>
        {
            var result = await Mediator.Send(new GetComplementGroupsQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("groups")]
    public Task<IActionResult> CreateGroup([FromBody] CreateComplementGroupCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(CreateGroup), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("groups/{id:long}")]
    public Task<IActionResult> UpdateGroup(long id, [FromBody] UpdateComplementGroupRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(UpdateGroup), async () =>
        {
            var result = await Mediator.Send(new UpdateComplementGroupCommand(
                id, request.Name, request.ComplementGroupTypeId, request.MinSelection, request.MaxSelection), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("groups/{id:long}/deactivate")]
    public Task<IActionResult> DeactivateGroup(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(DeactivateGroup), async () =>
        {
            var result = await Mediator.Send(new DeactivateComplementGroupCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // --- Complement (opção dentro de um grupo, ex.: "Coca-Cola" dentro de "Escolha uma bebida") ---

    [HttpPost("groups/{id:long}/complements")]
    public Task<IActionResult> AddComplement(long id, [FromBody] AddComplementRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(AddComplement), async () =>
        {
            var result = await Mediator.Send(new AddComplementCommand(id, request.ComplementItemId, request.ExtraPrice), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("groups/{id:long}/complements/{complementId:long}")]
    public Task<IActionResult> UpdateComplementPrice(long id, long complementId, [FromBody] UpdateComplementPriceRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(UpdateComplementPrice), async () =>
        {
            var result = await Mediator.Send(new UpdateComplementPriceCommand(id, complementId, request.ExtraPrice), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("groups/{id:long}/complements/{complementId:long}")]
    public Task<IActionResult> RemoveComplement(long id, long complementId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(RemoveComplement), async () =>
        {
            var result = await Mediator.Send(new RemoveComplementCommand(id, complementId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // --- ProductComplementGroup (vínculo produto x grupo) ---

    [HttpGet("products/{productId:long}")]
    public Task<IActionResult> GetProductGroups(long productId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(GetProductGroups), async () =>
        {
            var result = await Mediator.Send(new GetProductComplementGroupsQuery(productId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("products/{productId:long}/groups")]
    public Task<IActionResult> LinkProductGroup(long productId, [FromBody] LinkProductComplementGroupRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(LinkProductGroup), async () =>
        {
            var result = await Mediator.Send(new LinkProductComplementGroupCommand(productId, request.ComplementGroupId, request.DisplayOrder), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpDelete("product-groups/{productComplementGroupId:long}")]
    public Task<IActionResult> UnlinkProductGroup(long productComplementGroupId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ComplementsController), nameof(UnlinkProductGroup), async () =>
        {
            var result = await Mediator.Send(new UnlinkProductComplementGroupCommand(productComplementGroupId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

// Requests separados dos commands quando há parâmetro de rota.
public sealed record UpdateComplementItemRequest(string Name);
public sealed record UpdateComplementGroupRequest(string Name, long ComplementGroupTypeId, int MinSelection, int MaxSelection);
public sealed record AddComplementRequest(long ComplementItemId, decimal ExtraPrice);
public sealed record UpdateComplementPriceRequest(decimal ExtraPrice);
public sealed record LinkProductComplementGroupRequest(long ComplementGroupId, int DisplayOrder);
