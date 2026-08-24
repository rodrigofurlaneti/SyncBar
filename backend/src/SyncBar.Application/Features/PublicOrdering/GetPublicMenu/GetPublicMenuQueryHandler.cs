using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Catalog;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.PublicOrdering.GetPublicMenu;

internal sealed class GetPublicMenuQueryHandler(
    IDiningTableRepository diningTableRepository,
    IBranchRepository branchRepository,
    IProductRepository productRepository,
    IProductComplementGroupRepository productComplementGroupRepository,
    IComplementGroupRepository complementGroupRepository,
    IComplementItemRepository complementItemRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetPublicMenuQuery, PublicMenuResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<PublicMenuResponse>> Handle(GetPublicMenuQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetPublicMenuQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP do cliente lido no request, caso aplicável
            async (userIdBox) =>
            {
                // Como este é um endpoint de acesso público via QR Code, não temos um
                // usuário autenticado fazendo a consulta.

                var table = await diningTableRepository.GetByQrTokenAsync(request.Token, cancellationToken);
                if (table is null)
                    return Result.Failure<PublicMenuResponse>(new Error("DiningTable.InvalidToken", "Invalid or expired QR code."));

                var branch = await branchRepository.GetByIdAsync(table.BranchId, cancellationToken);
                if (branch is null || !branch.IsActive)
                    return Result.Failure<PublicMenuResponse>(new Error("Branch.NotFound", "Branch not found."));

                var products = await productRepository.GetByCompanyAsync(branch.CompanyId, cancellationToken);
                var productIds = products.Select(p => p.Id).ToList();

                // Fase 6a (extensão): mesmo cálculo em lote usado no cardápio interno — o cliente
                // no QR Code precisa ver os mesmos grupos de complemento pra escolher ao lançar o item.
                var complementsByProduct = await MenuComplementsBuilder.BuildAsync(
                    productIds, productComplementGroupRepository, complementGroupRepository, complementItemRepository, cancellationToken,
                    productRepository);

                var items = products
                    .OrderBy(p => p.CategoryId).ThenBy(p => p.Name)
                    .Select(p => new MenuItemResponse(
                        p.Id, p.CategoryId, p.UnitOfMeasureId, p.Name, p.Description, p.Barcode,
                        p.SalePrice, p.CostPrice, p.IsStockControlled, p.PreparationTimeMinutes, p.ImageUrl,
                        complementsByProduct.TryGetValue(p.Id, out var groups) ? groups : []))
                    .ToList();

                return Result.Success(new PublicMenuResponse(branch.Name, table.Number, items));
            });
    }
}
