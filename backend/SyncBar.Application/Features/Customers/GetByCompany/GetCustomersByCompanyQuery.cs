using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Customers.GetByCompany;

public sealed record GetCustomersByCompanyQuery(long CompanyId, string? Search) : IQuery<IReadOnlyCollection<CustomerResponse>>;
