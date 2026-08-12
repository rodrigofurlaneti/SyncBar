using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.SetLimits;

internal sealed class SetStockLimitsCommandHandler(
    IStockItemRepository stockItemRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetStockLimitsCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SetStockLimitsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetStockLimitsCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário ou gerente definindo os limites, preencha:
                // userIdBox.Value = request.UserId;

                var stockItem = await stockItemRepository.GetByIdForUpdateAsync(request.StockItemId, cancellationToken);
                if (stockItem is null || !stockItem.IsActive)
                    return Result.Failure(new Error("StockItem.NotFound", "Stock item not found."));

                var result = stockItem.SetLimits(request.MinimumQuantity, request.MaximumQuantity);
                if (result.IsFailure)
                    return result;

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}