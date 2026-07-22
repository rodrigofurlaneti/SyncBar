using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Catalog.CreateCategory;
using SyncBar.Application.Features.Catalog.CreateProduct;
using SyncBar.Application.Features.Catalog.DeactivateProduct;
using SyncBar.Application.Features.Catalog.GetCategories;
using SyncBar.Application.Features.Catalog.SetProductImage;
using SyncBar.Application.Features.Catalog.UpdateProduct;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Cardapio")]
public sealed class ProductsController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("categories/company/{companyId:long}")]
    public async Task<IActionResult> GetCategories(long companyId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCategoriesQuery(companyId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateProductCommand(
            id, request.CategoryId, request.UnitOfMeasureId, request.Name, request.Description,
            request.Barcode, request.SalePrice, request.CostPrice, request.IsStockControlled,
            request.PreparationTimeMinutes), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    // Upload da foto do produto (JPG/PNG/WebP ate 2 MB).
    [HttpPost("{id:long}/image")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(long id, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "Product.NoFile", Detail = "Envie um arquivo de imagem." });

        using var memory = new MemoryStream();
        await file.CopyToAsync(memory, ct);

        var result = await Mediator.Send(new SetProductImageCommand(
            id, Path.GetExtension(file.FileName), memory.ToArray()), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(new { imageUrl = result.Value });
    }

    [HttpPut("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeactivateProductCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
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
