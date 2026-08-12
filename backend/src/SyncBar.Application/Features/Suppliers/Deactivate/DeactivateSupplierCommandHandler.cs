using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Suppliers.Deactivate;

internal sealed class DeactivateSupplierCommandHandler(
    ISupplierRepository supplierRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeactivateSupplierCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeactivateSupplierCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeactivateSupplierCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gestor desativando o fornecedor, preencha:
                // userIdBox.Value = request.UserId;

                var supplier = await supplierRepository.GetByIdForUpdateAsync(request.SupplierId, cancellationToken);
                if (supplier is null || !supplier.IsActive)
                    return Result.Failure(new Error("Supplier.NotFound", "Supplier not found."));

                supplier.Deactivate();
                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}