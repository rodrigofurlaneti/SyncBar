using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.CustomerAddresses.GetByCustomerId
{
    internal sealed class GetCustomerAddressesByCustomerIdQueryHandler(
        ICustomerAddressRepository customerAddressRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseQueryHandler<GetCustomerAddressesByCustomerIdQuery, IEnumerable<CustomerAddressResponse>>(logRepository, unitOfWork)
    {
        public override async Task<Result<IEnumerable<CustomerAddressResponse>>> Handle(GetCustomerAddressesByCustomerIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetCustomerAddressesByCustomerIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    if (request.CustomerId <= 0)
                        return Result.Failure<IEnumerable<CustomerAddressResponse>>(new Error("CustomerAddress.InvalidCustomerId", "The provided Customer ID is invalid."));

                    var entities = await customerAddressRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

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
