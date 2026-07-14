using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Suppliers.Create;

internal sealed class CreateSupplierCommandHandler(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateSupplierCommand, long>
{
    public async Task<Result<long>> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = Supplier.Create(
            request.CompanyId, request.LegalName, request.TradeName, request.Cnpj, request.Email, request.Phone);
        if (supplier.IsFailure)
            return Result.Failure<long>(supplier.Error);

        await supplierRepository.AddAsync(supplier.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(supplier.Value.Id);
    }
}
