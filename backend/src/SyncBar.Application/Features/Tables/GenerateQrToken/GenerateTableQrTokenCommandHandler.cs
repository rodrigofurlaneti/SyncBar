using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Tables.GenerateQrToken;

internal sealed class GenerateTableQrTokenCommandHandler : BaseCommandHandler<GenerateTableQrTokenCommand, Guid>
{
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateTableQrTokenCommandHandler(
        IDiningTableRepository diningTableRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _diningTableRepository = diningTableRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<Guid>> Handle(GenerateTableQrTokenCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GenerateTableQrTokenCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente gerando o novo token, preencha:

                var table = await _diningTableRepository.GetByIdForUpdateAsync(request.DiningTableId, cancellationToken);
                if (table is null || !table.IsActive)
                    return Result.Failure<Guid>(new Error("DiningTable.NotFound", "Dining table not found."));

                var token = table.GenerateQrToken();
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(token);
            });
    }
}