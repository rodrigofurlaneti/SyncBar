using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Suppliers.GetByCompany;

public sealed record GetSuppliersByCompanyQuery(long CompanyId) : IQuery<IReadOnlyCollection<SupplierResponse>>;
