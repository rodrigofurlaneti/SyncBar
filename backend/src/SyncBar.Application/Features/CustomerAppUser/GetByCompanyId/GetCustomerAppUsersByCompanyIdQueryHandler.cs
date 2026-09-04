using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.CustomerAppUser.GetByCompanyId
{
    internal sealed class GetCustomerAppUsersByCompanyIdQueryHandler(
        ICustomerAppUserRepository customerAppUserRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseQueryHandler<GetCustomerAppUsersByCompanyIdQuery, IEnumerable<CustomerAppUserResponse>>(logRepository, unitOfWork)
    {
        public override async Task<Result<IEnumerable<CustomerAppUserResponse>>> Handle(GetCustomerAppUsersByCompanyIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetCustomerAppUsersByCompanyIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    if (request.CompanyId <= 0)
                        return Result.Failure<IEnumerable<CustomerAppUserResponse>>(new Error("CustomerAppUser.InvalidCompanyId", "The provided Company ID is invalid."));
                    var entities = await customerAppUserRepository.GetByCompanyId(request.CompanyId, cancellationToken);
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
