using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Users.Deactivate;

internal sealed class DeactivateUserCommandHandler(
    IAppUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateUserCommand>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdForUpdateAsync(request.AppUserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure(new Error("AppUser.NotFound", "User not found."));

        user.Deactivate();
        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
