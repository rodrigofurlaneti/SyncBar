using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Customers.GetByCompany;

internal sealed class GetCustomersByCompanyQueryHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomersByCompanyQuery, IReadOnlyCollection<CustomerResponse>>
{
    public async Task<Result<IReadOnlyCollection<CustomerResponse>>> Handle(
        GetCustomersByCompanyQuery request, CancellationToken cancellationToken)
    {
        var customers = string.IsNullOrWhiteSpace(request.Search)
            ? await customerRepository.GetByCompanyAsync(request.CompanyId, cancellationToken)
            : await customerRepository.SearchAsync(request.CompanyId, request.Search.Trim(), cancellationToken);

        IReadOnlyCollection<CustomerResponse> response = customers
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Phone, c.Cpf, c.Email, c.LoyaltyPoints, c.IsActive))
            .ToList();

        return Result.Success(response);
    }
}
