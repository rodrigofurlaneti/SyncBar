using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.CustomerAddresses.Remove
{
    internal sealed class RemoveCustomerAddressCommandHandler(
        ICustomerAddressRepository customerAddressRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseCommandHandler<RemoveCustomerAddressCommand>(logRepository, unitOfWork)
    {
        public override async Task<Result> Handle(RemoveCustomerAddressCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RemoveCustomerAddressCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await customerAddressRepository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null || !entity.IsActive)
                        return Result.Failure(new Error("CustomerAddress.NotFound", "Customer address not found."));

                    await customerAddressRepository.RemoveAsync(request.Id, cancellationToken);
                    await unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
