using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.CustomerAddresses.GetById
{
    internal sealed class GetCustomerAddressByIdQueryHandler(
        ICustomerAddressRepository customerAddressRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseQueryHandler<GetCustomerAddressByIdQuery, CustomerAddressResponse>(logRepository, unitOfWork)
    {
        public override async Task<Result<CustomerAddressResponse>> Handle(GetCustomerAddressByIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetCustomerAddressByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    if (request.Id <= 0)
                        return Result.Failure<CustomerAddressResponse>(new Error("CustomerAddress.InvalidId", "The provided ID is invalid."));

                    var entity = await customerAddressRepository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null || !entity.IsActive)
                        return Result.Failure<CustomerAddressResponse>(new Error("CustomerAddress.NotFound", "Customer address not found."));

                    var response = new CustomerAddressResponse(
                        entity.Id,
                        entity.CompanyId,
                        entity.BranchId,
                        entity.CustomerId,
                        entity.LastOrderId,
                        entity.Street,
                        entity.Number,
                        entity.Supplement,
                        entity.LastOrderAt,
                        entity.IsActive,
                        entity.CreatedAt
                    );

                    return Result.Success(response);
                });
        }
    }
}
