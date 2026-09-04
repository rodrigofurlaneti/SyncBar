using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.PublicOrdering.GetPublicMenu;
using SyncBar.Application.Features.Storefront.AddOrder;
using SyncBar.Application.Features.Storefront.GetBranchMenu;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using System.Linq;
using System.Text.Json.Serialization;

namespace SyncBar.API.Controllers
{
    [AllowAnonymous]
    [EnableRateLimiting("public-ordering")]
    [Route("api/storefront")]
    public sealed class StorefrontController(
            IMediator mediator,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork) : ApiController(mediator)
    {
        [HttpGet("branches/{branchId:long}/menu")]
        public Task<IActionResult> GetBranchMenu(long branchId, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(StorefrontController), nameof(GetBranchMenu), async () =>
            {
                var result = await Mediator.Send(new GetBranchMenuQuery(branchId), ct);
                return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
            });

        [HttpPost("branches/{branchId:long}/orders")]
        public Task<IActionResult> CreateOrder(long branchId, [FromBody] AddWebStorefrontOrderRequest request, CancellationToken ct) =>
                    ExecuteWithLogAsync(logRepository, unitOfWork, nameof(StorefrontController), nameof(CreateOrder), async () =>
                    {
                        var commandItems = request.Items.Select(i => new WebStorefrontItemDto(
                            i.ProductId, i.Quantity, i.Notes, i.Complements)).ToList();

                        var result = await Mediator.Send(new AddWebStorefrontOrderCommand(
                            branchId,
                            request.CustomerId,
                            request.CustomerName,
                            request.CustomerPhone,
                            request.GeneralNotes,
                            commandItems), ct);

                        return result.IsFailure ? HandleFailure(result) : Ok(new { orderId = result.Value });
                    });
    }

    public sealed record AddWebStorefrontOrderRequest(
            long? CustomerId, 
            [property: JsonRequired] string CustomerName,
            string? CustomerPhone,
            string? GeneralNotes,
            [property: JsonRequired] IReadOnlyCollection<WebStorefrontItemRequest> Items);

    public sealed record WebStorefrontItemRequest(
        [property: JsonRequired] long ProductId,
        [property: JsonRequired] decimal Quantity,
        string? Notes,
        IReadOnlyCollection<OrderItemComplementSelection>? Complements = null);
}