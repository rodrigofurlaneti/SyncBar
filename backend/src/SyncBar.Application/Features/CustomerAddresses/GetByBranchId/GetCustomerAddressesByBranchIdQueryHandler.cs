using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.CustomerAddresses.GetByBranchId
{
    internal sealed class GetCustomerAddressesByBranchIdQueryHandler(
        ICustomerAddressRepository customerAddressRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseQueryHandler<GetCustomerAddressesByBranchIdQuery, IEnumerable<CustomerAddressResponse>>(logRepository, unitOfWork)
    {
        public override async Task<Result<IEnumerable<CustomerAddressResponse>>> Handle(GetCustomerAddressesByBranchIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetCustomerAddressesByBranchIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    if (request.BranchId <= 0)
                        return Result.Failure<IEnumerable<CustomerAddressResponse>>(new Error("CustomerAddress.InvalidBranchId", "The provided Branch ID is invalid."));
                    var entities = await customerAddressRepository.GetByBranchIdAsync(request.BranchId, cancellationToken);
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
