using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Catalog.Pizza.AddPizzaCrust;
using SyncBar.Application.Features.Catalog.Pizza.AddPizzaEdge;
using SyncBar.Application.Features.Catalog.Pizza.AddPizzaSize;
using SyncBar.Application.Features.Catalog.Pizza.CreatePizzaConfiguration;
using SyncBar.Application.Features.Catalog.Pizza.CreatePizzaFlavor;
using SyncBar.Application.Features.Catalog.Pizza.SetPizzaFlavorPrice;
using SyncBar.Application.Features.Integrations.IFood.Catalog.Pizza;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

// Fase 17 (pizza) — cadastro de sabores e configuração de pizza (tamanhos/bordas/recheios de
// borda/preço por sabor×tamanho) de um Product, e o gatilho manual de sincronização com o iFood.
// Mesma policy de ComplementsController — é parte do cardápio.
[Authorize(Policy = "Feature:Cardapio")]
[Route("api/pizza")]
public sealed class PizzaController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    // --- PizzaFlavor (cadastro de sabor, reaproveitável entre pizzas) ---

    [HttpPost("flavors")]
    public Task<IActionResult> CreateFlavor([FromBody] CreatePizzaFlavorCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PizzaController), nameof(CreateFlavor), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // --- PizzaConfiguration (1:1 com Product) ---

    [HttpPost("configurations")]
    public Task<IActionResult> CreateConfiguration([FromBody] CreatePizzaConfigurationCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PizzaController), nameof(CreateConfiguration), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("configurations/{id:long}/sizes")]
    public Task<IActionResult> AddSize(long id, [FromBody] AddPizzaSizeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PizzaController), nameof(AddSize), async () =>
        {
            var result = await Mediator.Send(new AddPizzaSizeCommand(id, request.Name, request.Slices, request.AcceptedFractions, request.DisplayOrder), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("configurations/{id:long}/crusts")]
    public Task<IActionResult> AddCrust(long id, [FromBody] AddPizzaCrustRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PizzaController), nameof(AddCrust), async () =>
        {
            var result = await Mediator.Send(new AddPizzaCrustCommand(id, request.Name, request.ExtraPrice, request.DisplayOrder), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("configurations/{id:long}/edges")]
    public Task<IActionResult> AddEdge(long id, [FromBody] AddPizzaEdgeRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PizzaController), nameof(AddEdge), async () =>
        {
            var result = await Mediator.Send(new AddPizzaEdgeCommand(id, request.Name, request.ExtraPrice, request.DisplayOrder), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("configurations/{id:long}/flavor-prices")]
    public Task<IActionResult> SetFlavorPrice(long id, [FromBody] SetPizzaFlavorPriceRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PizzaController), nameof(SetFlavorPrice), async () =>
        {
            var result = await Mediator.Send(new SetPizzaFlavorPriceCommand(id, request.PizzaFlavorId, request.PizzaSizeId, request.Price), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // --- Sincronização manual com o iFood (Catalog v1, legado) ---

    [HttpPost("configurations/{id:long}/ifood-sync")]
    public Task<IActionResult> SyncWithIFood(long id, [FromBody] SyncIFoodPizzaRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PizzaController), nameof(SyncWithIFood), async () =>
        {
            var result = await Mediator.Send(new SyncIFoodPizzaCommand(request.BranchId, id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}

// Requests separados dos commands quando há parâmetro de rota.
// Fase Sonar MEDIUM (2026-08-24): [property: JsonRequired] nos campos de tipo valor para
// evitar under-posting.
public sealed record AddPizzaSizeRequest(
    string Name, int? Slices,
    [property: JsonRequired] int AcceptedFractions,
    [property: JsonRequired] int DisplayOrder);
public sealed record AddPizzaCrustRequest(
    string Name, [property: JsonRequired] decimal ExtraPrice, [property: JsonRequired] int DisplayOrder);
public sealed record AddPizzaEdgeRequest(
    string Name, [property: JsonRequired] decimal ExtraPrice, [property: JsonRequired] int DisplayOrder);
public sealed record SetPizzaFlavorPriceRequest(
    [property: JsonRequired] long PizzaFlavorId,
    [property: JsonRequired] long PizzaSizeId,
    [property: JsonRequired] decimal Price);
public sealed record SyncIFoodPizzaRequest([property: JsonRequired] long BranchId);
