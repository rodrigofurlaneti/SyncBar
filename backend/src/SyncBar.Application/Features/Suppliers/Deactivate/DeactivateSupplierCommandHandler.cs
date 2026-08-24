using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Suppliers.Deactivate;

internal sealed class DeactivateSupplierCommandHandler : BaseCommandHandler<DeactivateSupplierCommand>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateSupplierCommandHandler(
        ISupplierRepository supplierRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(DeactivateSupplierCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeactivateSupplierCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gestor desativando o fornecedor, preencha:

                var supplier = await _supplierRepository.GetByIdForUpdateAsync(request.SupplierId, cancellationToken);
                if (supplier is null || !supplier.IsActive)
                    return Result.Failure(new Error("Supplier.NotFound", "Supplier not found."));

                supplier.Deactivate();
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}