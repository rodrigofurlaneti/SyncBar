using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Users.Deactivate;

internal sealed class DeactivateUserCommandHandler : BaseCommandHandler<DeactivateUserCommand>
{
    private readonly IAppUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserCommandHandler(
        IAppUserRepository userRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeactivateUserCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do administrador que está desativando o usuário, preencha:
                // userIdBox.Value = request.AdminUserId;

                var user = await _userRepository.GetByIdForUpdateAsync(request.AppUserId, cancellationToken);
                if (user is null || !user.IsActive)
                    return Result.Failure(new Error("AppUser.NotFound", "User not found."));

                user.Deactivate();
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}