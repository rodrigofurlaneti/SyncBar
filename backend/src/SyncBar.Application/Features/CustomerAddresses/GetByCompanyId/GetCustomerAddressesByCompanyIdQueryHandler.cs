using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.CustomerAddresses.GetByCompanyId
{
    internal sealed class GetCustomerAddressesByCompanyIdQueryHandler(
        ICustomerAddressRepository customerAddressRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseQueryHandler<GetCustomerAddressesByCompanyIdQuery, IEnumerable<CustomerAddressResponse>>(logRepository, unitOfWork)
    {
        public override async Task<Result<IEnumerable<CustomerAddressResponse>>> Handle(GetCustomerAddressesByCompanyIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetCustomerAddressesByCompanyIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    if (request.CompanyId <= 0)
                        return Result.Failure<IEnumerable<CustomerAddressResponse>>(new Error("CustomerAddress.InvalidCompanyId", "The provided Company ID is invalid."));
                    var entities = await customerAddressRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
                    var response = entities
                        .Where(x => x is not null && x.IsActive)
                        .Select(x => new CustomerAddressResponse(
                            x!.Id,
                            x.CompanyId,
                            x.BranchId,
                            x.CustomerId,
                            x.LastOrderId,
                            x.Street,
                            x.Number,
                            x.Supplement,
                            x.ZipCode,
                            x.LastOrderAt,
                            x.IsActive,
                            x.CreatedAt
                        ))
                        .ToList();
                    return Result.Success<IEnumerable<CustomerAddressResponse>>(response);
                });
        }
    }
}
