using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.CustomerAppUser.Remove
{
    internal sealed class RemoveCustomerAppUserCommandHandler(
        ICustomerAppUserRepository customerAppUserRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseCommandHandler<RemoveCustomerAppUserCommand>(logRepository, unitOfWork)
    {
        public override async Task<Result> Handle(RemoveCustomerAppUserCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RemoveCustomerAppUserCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await customerAppUserRepository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null || !entity.IsActive)
                        return Result.Failure(new Error("CustomerAppUser.NotFound", "Customer app user not found."));
                    await customerAppUserRepository.RemoveAsync(request.Id, cancellationToken);
                    await unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success();
                });
        }
    }
}
