using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.CustomerAppUser.GetByCustomerId
{
    internal sealed class GetCustomerAppUsersByCustomerIdQueryHandler(
        ICustomerAppUserRepository customerAppUserRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseQueryHandler<GetCustomerAppUsersByCustomerIdQuery, IEnumerable<CustomerAppUserResponse>>(logRepository, unitOfWork)
    {
        public override async Task<Result<IEnumerable<CustomerAppUserResponse>>> Handle(GetCustomerAppUsersByCustomerIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetCustomerAppUsersByCustomerIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    if (request.CustomerId <= 0)
                        return Result.Failure<IEnumerable<CustomerAppUserResponse>>(new Error("CustomerAppUser.InvalidCustomerId", "The provided Customer ID is invalid."));
                    var entities = await customerAppUserRepository.GetByCustomerId(request.CustomerId, cancellationToken);
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
