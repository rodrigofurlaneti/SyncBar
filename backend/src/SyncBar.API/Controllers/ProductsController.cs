using System.Diagnostics;
using System.IO;
using SyncBar.Application.Features.Catalog.ProductExtras;
using System.Security.Claims;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Catalog.ActivateProduct;
using SyncBar.Application.Features.Catalog.CreateProduct;
using SyncBar.Application.Features.Catalog.DeactivateProduct;
using SyncBar.Application.Features.Catalog.GetMenuForManagement;
using SyncBar.Application.Features.Catalog.SetProductImage;
using SyncBar.Application.Features.Catalog.UpdateProduct;
using SyncBar.Application.Features.Catalog.GetProductById;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class ProductsController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [Authorize(Roles = ManagerRoles)]
    [HttpGet("{id:long}/optional-extras")]
    public async Task<IActionResult> ListOptionalExtra(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOptionalExtrasByProductIdQuery(id), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }
    [Authorize(Roles = ManagerRoles)]
    [HttpPost("{id:long}/optional-extras")]
    public async Task<IActionResult> AddOptionalExtra(long id, [FromBody] OptionalExtraRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new AddOptionalExtraCommand(id, request.OptionalExtraName, request.DisplayOrder), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}/optional-extras/{itemId:long}")]
    public async Task<IActionResult> UpdateOptionalExtra(long id, long itemId, [FromBody] OptionalExtraRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateOptionalExtraCommand(id, itemId, request.OptionalExtraName, request.DisplayOrder), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
    [Authorize(Roles = ManagerRoles)]
    [HttpDelete("{id:long}/optional-extras/{itemId:long}")]
    public async Task<IActionResult> DeleteOptionalExtra(long id, long itemId, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeleteOptionalExtraCommand(id, itemId), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
    [Authorize(Roles = ManagerRoles)]
    [HttpGet("{id:long}/boosts")]
    public async Task<IActionResult> ListProductBoost(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetProductBoostsByProductIdQuery(id), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }
    [Authorize(Roles = ManagerRoles)]
    [HttpPost("{id:long}/boosts")]
    public async Task<IActionResult> AddProductBoost(long id, [FromBody] ProductBoostRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new AddProductBoostCommand(id, request.BoostName, request.IncrementalValue, request.DisplayOrder), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}/boosts/{itemId:long}")]
    public async Task<IActionResult> UpdateProductBoost(long id, long itemId, [FromBody] ProductBoostRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateProductBoostCommand(id, itemId, request.BoostName, request.IncrementalValue, request.DisplayOrder), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
    [Authorize(Roles = ManagerRoles)]
    [HttpDelete("{id:long}/boosts/{itemId:long}")]
    public async Task<IActionResult> DeleteProductBoost(long id, long itemId, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeleteProductBoostCommand(id, itemId), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}/extras-and-boosts")]
    public async Task<IActionResult> ToggleExtras(long id, [FromBody] ProductExtrasRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new ToggleProductExtrasAndBoostsCommand(id, request.HasOptionalExtras, request.HasBoosts), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    public sealed record OptionalExtraRequest(string OptionalExtraName, [property: JsonRequired] int DisplayOrder);
    public sealed record ProductBoostRequest(string BoostName, [property: JsonRequired] decimal IncrementalValue, [property: JsonRequired] int DisplayOrder);
    public sealed record ProductExtrasRequest([property: JsonRequired] bool HasOptionalExtras, [property: JsonRequired] bool HasBoosts);
    [Authorize(Roles = ManagerRoles)]
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
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

    [HttpGet("company/{companyId:long}/management")]
    public Task<IActionResult> GetForManagement(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(GetForManagement), async () =>
        {
            var result = await Mediator.Send(new GetMenuForManagementQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}/deactivate")]
    public Task<IActionResult> Deactivate(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(Deactivate), async () =>
        {
            var result = await Mediator.Send(new DeactivateProductCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}/activate")]
    public Task<IActionResult> Activate(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ProductsController), nameof(Activate), async () =>
        {
            var result = await Mediator.Send(new ActivateProductCommand(id), ct);
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
