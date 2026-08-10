using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Catalog.CreateCategory;
using SyncBar.Application.Features.Catalog.CreateProduct;
using SyncBar.Application.Features.Catalog.DeactivateProduct;
using SyncBar.Application.Features.Catalog.GetCategories;
using SyncBar.Application.Features.Catalog.SetProductImage;
using SyncBar.Application.Features.Catalog.UpdateProduct;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Cardapio")]
public sealed class ProductsController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("categories/company/{companyId:long}")]
    public Task<IActionResult> GetCategories(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(GetCategories), async () =>
        {
            var result = await Mediator.Send(new GetCategoriesQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("categories")]
    public Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(CreateCategory), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

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

    // Upload da foto do produto (JPG/PNG/WebP ate 2 MB).
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

    [HttpPut("{id:long}/deactivate")]
    public Task<IActionResult> Deactivate(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(Deactivate), async () =>
        {
            var result = await Mediator.Send(new DeactivateProductCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateProductRequest(
    long CategoryId,
    long UnitOfMeasureId,
    string Name,
    string? Description,
    string? Barcode,
    decimal SalePrice,
    decimal? CostPrice,
    bool IsStockControlled,
    int? PreparationTimeMinutes);