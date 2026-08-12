using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Suppliers.Create;

internal sealed class CreateSupplierCommandHandler(
    ISupplierRepository supplierRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CreateSupplierCommand, long>(logRepository, unitOfWork)
{
    public override async Task<Result<long>> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreateSupplierCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gestor criando o fornecedor, preencha:
                // userIdBox.Value = request.UserId;

                var supplier = Supplier.Create(
                    request.CompanyId, request.LegalName, request.TradeName, request.Cnpj, request.Email, request.Phone);

                if (supplier.IsFailure)
                    return Result.Failure<long>(supplier.Error);

                await supplierRepository.AddAsync(supplier.Value, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(supplier.Value.Id);
            });
    }
}