using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.CustomerAppUser.GetById
{
    internal sealed class GetCustomerAppUserByIdQueryHandler(
        ICustomerAppUserRepository customerAppUserRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseQueryHandler<GetCustomerAppUserByIdQuery, CustomerAppUserResponse>(logRepository, unitOfWork)
    {
        public override async Task<Result<CustomerAppUserResponse>> Handle(GetCustomerAppUserByIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetCustomerAppUserByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await customerAppUserRepository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null || !entity.IsActive)
                        return Result.Failure<CustomerAppUserResponse>(new Error("CustomerAppUser.NotFound", "Customer app user not found."));
                    var response = new CustomerAppUserResponse(
                        entity.Id,
                        entity.CompanyId,
                        entity.BranchId,
                        entity.CustomerId,
                        entity.UserName,
                        entity.Email,
                        entity.IsActive,
                        entity.CreatedAt,
                        entity.LastLoginAt
                    );
                    return Result.Success(response);
                });
        }
    }
}
