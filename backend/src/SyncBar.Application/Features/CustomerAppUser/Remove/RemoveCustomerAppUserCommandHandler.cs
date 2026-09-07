using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.CustomerAppUser.Remove
{
    internal sealed class RemoveCustomerAppUserCommandHandler : BaseCommandHandler<RemoveCustomerAppUserCommand>
    {
        private readonly ICustomerAppUserRepository _customerAppUserRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveCustomerAppUserCommandHandler(
            ICustomerAppUserRepository customerAppUserRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _customerAppUserRepository = customerAppUserRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(RemoveCustomerAppUserCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RemoveCustomerAppUserCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await _customerAppUserRepository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null || !entity.IsActive)
                        return Result.Failure(new Error("CustomerAppUser.NotFound", "Customer app user not found."));
                    await _customerAppUserRepository.RemoveAsync(request.Id, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);
                    return Result.Success();
                });
        }
    }
}
