using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Catalog.CreateCategory;
using SyncBar.Application.Features.Catalog.DeactivateCategory;
using SyncBar.Application.Features.Catalog.GetCategories;
using SyncBar.Application.Features.Catalog.GetCategoryById;
using SyncBar.Application.Features.Catalog.UpdateCategory;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

// Protege a classe inteira (mesmo padrão do ProductsController) — criar/editar/desativar
// categoria exige login de Gerente/Admin.
[Authorize(Roles = "Administrador,Gerente")]
public sealed class CategoriesController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    // AllowAnonymous preservado — rota migrada de ProductsController.GetCategories, onde já
    // tinha essa exceção (usada por telas que listam categorias sem exigir login). Mantido
    // igual na migração para não mudar comportamento de quem já consome essa rota.
    [AllowAnonymous]
    [HttpGet("company/{companyId:long}")]
    public Task<IActionResult> GetByCompany(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CategoriesController), nameof(GetByCompany), async () =>
        {
            var result = await Mediator.Send(new GetCategoriesQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("{id:long}")]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CategoriesController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetCategoryByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateCategoryCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CategoriesController), nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("{id:long}")]
    public Task<IActionResult> Update(long id, [FromBody] UpdateCategoryRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CategoriesController), nameof(Update), async () =>
        {
            var result = await Mediator.Send(new UpdateCategoryCommand(id, request.Name, request.DisplayOrder), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/deactivate")]
    public Task<IActionResult> Deactivate(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CategoriesController), nameof(Deactivate), async () =>
        {
            var result = await Mediator.Send(new DeactivateCategoryCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateCategoryRequest(
    [property: JsonRequired] string Name,
    [property: JsonRequired] int DisplayOrder);
