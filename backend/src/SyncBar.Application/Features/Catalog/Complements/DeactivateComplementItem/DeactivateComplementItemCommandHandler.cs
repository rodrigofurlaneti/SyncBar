using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.DeactivateComplementItem;

internal sealed class DeactivateComplementItemCommandHandler : BaseCommandHandler<DeactivateComplementItemCommand>
{
    private readonly IComplementItemRepository _complementItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateComplementItemCommandHandler(
        IComplementItemRepository complementItemRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _complementItemRepository = complementItemRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result> Handle(DeactivateComplementItemCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(DeactivateComplementItemCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var complementItem = await _complementItemRepository.GetByIdForUpdateAsync(request.ComplementItemId, cancellationToken);
                if (complementItem is null || !complementItem.IsActive)
                    return Result.Failure(new Error("ComplementItem.NotFound", "Complement item not found."));

                // Nota: não desativa em cascata os Complement (opções) que usam este item em
                // algum ComplementGroup — quem gerencia o grupo decide se remove a opção
                // (RemoveComplement). Evita quebrar grupos ativos por engano.
                complementItem.Deactivate();
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            });
}
