using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Catalog.CreateProduct;
using SyncBar.Application.Features.Catalog.DeactivateProduct;
using SyncBar.Application.Features.Catalog.SetProductImage;
using SyncBar.Application.Features.Catalog.UpdateProduct;
using SyncBar.Application.Features.Catalog.GetProductById;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

// 1. Protege a classe inteira para garantir que criação/edição exigem login de Gerente/Admin
[Authorize(Roles = "Administrador,Gerente")]
public sealed class ProductsController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    // As rotas de categoria (listar/criar/editar/desativar) foram movidas pro
    // CategoriesController dedicado — ver Cartão "CRUD de Categorias de Produtos". As duas
    // rotas que viviam aqui (GET categories/company/{id} e POST categories) saíram; o único
    // consumidor era features/catalog/api.ts no front, que foi atualizado junto para apontar
    // pra /api/categories/... — não sobrou nenhum outro lugar chamando as rotas antigas.

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("{id:long}")]
    public Task<IActionResult> Update(long id, [FromBody] UpdateProductRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(Update), async () =>
        {
            var result = await Mediator.Send(new UpdateProductCommand(
                id, request.CategoryId, request.UnitOfMeasureId, request.Name, request.Description,
                request.Barcode, request.SalePrice, request.CostPrice, request.IsStockControlled,
                request.PreparationTimeMinutes), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/image")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public Task<IActionResult> UploadImage(long id, IFormFile? file, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(UploadImage), async () =>
        {
            if (file is null || file.Length == 0)
                return BadRequest(new ProblemDetails { Title = "Product.NoFile", Detail = "Envie um arquivo de imagem." });
            using var memory = new MemoryStream();
            await file.CopyToAsync(memory, ct);
            var result = await Mediator.Send(new SetProductImageCommand(
                id, Path.GetExtension(file.FileName), memory.ToArray()), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { imageUrl = result.Value });
        });

    [HttpGet("{id:long}")]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetProductByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("{id:long}/deactivate")]
    public Task<IActionResult> Deactivate(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(Deactivate), async () =>
        {
            var result = await Mediator.Send(new DeactivateProductCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateProductRequest(
    [property: JsonRequired] long CategoryId,
    [property: JsonRequired] long UnitOfMeasureId,
    string Name,
    string? Description,
    string? Barcode,
    [property: JsonRequired] decimal SalePrice,
    decimal? CostPrice,
    [property: JsonRequired] bool IsStockControlled,
    int? PreparationTimeMinutes);