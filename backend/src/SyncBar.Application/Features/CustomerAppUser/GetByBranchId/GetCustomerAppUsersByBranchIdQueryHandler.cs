using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.CustomerAppUser.GetByBranchId
{
    internal sealed class GetCustomerAppUsersByBranchIdQueryHandler(
        ICustomerAppUserRepository customerAppUserRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseQueryHandler<GetCustomerAppUsersByBranchIdQuery, IEnumerable<CustomerAppUserResponse>>(logRepository, unitOfWork)
    {
        public override async Task<Result<IEnumerable<CustomerAppUserResponse>>> Handle(GetCustomerAppUsersByBranchIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetCustomerAppUsersByBranchIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    if (request.BranchId <= 0)
                        return Result.Failure<IEnumerable<CustomerAppUserResponse>>(new Error("CustomerAppUser.InvalidBranchId", "The provided Branch ID is invalid."));
                    var entities = await customerAppUserRepository.GetByBranchId(request.BranchId, cancellationToken);
                    var response = entities
                        .Where(x => x is not null && x.IsActive)
                        .Select(x => new CustomerAppUserResponse(
                            x!.Id,
                            x.CompanyId,
                            x.BranchId,
                            x.CustomerId,
                            x.UserName,
                            x.Email,
                            x.IsActive,
                            x.CreatedAt,
                            x.LastLoginAt
                        ))
                        .ToList();
                    return Result.Success<IEnumerable<CustomerAppUserResponse>>(response);
                });
        }
    }
}
