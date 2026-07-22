using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Purchases.GetByBranch;

internal sealed class GetPurchasesByBranchQueryHandler(IPurchaseRepository purchaseRepository)
    : IQueryHandler<GetPurchasesByBranchQuery, IReadOnlyCollection<PurchaseResponse>>
{
    public async Task<Result<IReadOnlyCollection<PurchaseResponse>>> Handle(
        GetPurchasesByBranchQuery request, CancellationToken cancellationToken)
    {
        var purchases = await purchaseRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        IReadOnlyCollection<PurchaseResponse> response = purchases
            .Select(p => new PurchaseResponse(
                p.Id, p.SupplierId, p.DocumentNumber, p.PurchasedAt, p.TotalAmount, p.Notes,
                p.Items.Select(i => new PurchaseItemResponse(i.ProductId, i.Quantity, i.UnitCost, i.TotalCost)).ToList()))
            .ToList();

        return Result.Success(response);
    }
}
