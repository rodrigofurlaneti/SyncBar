using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Tables.GenerateQrToken;

internal sealed class GenerateTableQrTokenCommandHandler(IDiningTableRepository diningTableRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<GenerateTableQrTokenCommand, Guid>
{
    public async Task<Result<Guid>> Handle(GenerateTableQrTokenCommand request, CancellationToken cancellationToken)
    {
        var table = await diningTableRepository.GetByIdForUpdateAsync(request.DiningTableId, cancellationToken);
        if (table is null || !table.IsActive)
            return Result.Failure<Guid>(new Error("DiningTable.NotFound", "Dining table not found."));

        var token = table.GenerateQrToken();
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(token);
    }
}
