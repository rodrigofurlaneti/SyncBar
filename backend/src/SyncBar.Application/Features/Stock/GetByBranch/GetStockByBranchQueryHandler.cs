using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.GetByBranch;

internal sealed class GetStockByBranchQueryHandler(
    IStockItemRepository stockItemRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetStockByBranchQuery, IReadOnlyCollection<StockItemResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<StockItemResponse>>> Handle(
        GetStockByBranchQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetStockByBranchQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente que está consultando o estoque, preencha:
                // userIdBox.Value = request.UserId;

                var items = await stockItemRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                IReadOnlyCollection<StockItemResponse> response = items
                    .OrderBy(i => i.ProductId)
                    .Select(i => new StockItemResponse(
                        i.Id, i.BranchId, i.ProductId, i.CurrentQuantity,
                        i.MinimumQuantity, i.MaximumQuantity, i.IsBelowMinimum()))
                    .ToList();

                return Result.Success(response);
            });
    }
}