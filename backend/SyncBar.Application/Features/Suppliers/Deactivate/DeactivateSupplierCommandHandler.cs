using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Suppliers.Deactivate;

internal sealed class DeactivateSupplierCommandHandler(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateSupplierCommand>
{
    public async Task<Result> Handle(DeactivateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await supplierRepository.GetByIdForUpdateAsync(request.SupplierId, cancellationToken);
        if (supplier is null || !supplier.IsActive)
            return Result.Failure(new Error("Supplier.NotFound", "Supplier not found."));

        supplier.Deactivate();
        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
