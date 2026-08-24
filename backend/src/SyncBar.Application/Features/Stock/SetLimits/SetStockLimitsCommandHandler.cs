using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Stock.SetLimits;

internal sealed class SetStockLimitsCommandHandler : BaseCommandHandler<SetStockLimitsCommand>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetStockLimitsCommandHandler(
        IStockItemRepository stockItemRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _stockItemRepository = stockItemRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(SetStockLimitsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetStockLimitsCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário ou gerente definindo os limites, preencha:

                var stockItem = await _stockItemRepository.GetByIdForUpdateAsync(request.StockItemId, cancellationToken);
                if (stockItem is null || !stockItem.IsActive)
                    return Result.Failure(new Error("StockItem.NotFound", "Stock item not found."));

                var result = stockItem.SetLimits(request.MinimumQuantity, request.MaximumQuantity);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}