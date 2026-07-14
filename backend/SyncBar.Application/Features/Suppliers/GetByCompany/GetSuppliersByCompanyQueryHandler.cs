using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Suppliers.GetByCompany;

internal sealed class GetSuppliersByCompanyQueryHandler(ISupplierRepository supplierRepository)
    : IQueryHandler<GetSuppliersByCompanyQuery, IReadOnlyCollection<SupplierResponse>>
{
    public async Task<Result<IReadOnlyCollection<SupplierResponse>>> Handle(
        GetSuppliersByCompanyQuery request, CancellationToken cancellationToken)
    {
        var suppliers = await supplierRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

        IReadOnlyCollection<SupplierResponse> response = suppliers
            .Select(s => new SupplierResponse(s.Id, s.LegalName, s.TradeName, s.Cnpj, s.Email, s.Phone, s.IsActive))
            .ToList();

        return Result.Success(response);
    }
}
