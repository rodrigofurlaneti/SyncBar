using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Tables.GenerateQrToken;

internal sealed class GenerateTableQrTokenCommandHandler(
    IDiningTableRepository diningTableRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<GenerateTableQrTokenCommand, Guid>(logRepository, unitOfWork)
{
    public override async Task<Result<Guid>> Handle(GenerateTableQrTokenCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GenerateTableQrTokenCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente gerando o novo token, preencha:
                // userIdBox.Value = request.UserId;

                var table = await diningTableRepository.GetByIdForUpdateAsync(request.DiningTableId, cancellationToken);
                if (table is null || !table.IsActive)
                    return Result.Failure<Guid>(new Error("DiningTable.NotFound", "Dining table not found."));

                var token = table.GenerateQrToken();
                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(token);
            });
    }
}