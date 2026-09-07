using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.CustomerAddresses.Remove
{
    internal sealed class RemoveCustomerAddressCommandHandler : BaseCommandHandler<RemoveCustomerAddressCommand>
    {
        private readonly ICustomerAddressRepository _customerAddressRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveCustomerAddressCommandHandler(
            ICustomerAddressRepository customerAddressRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _customerAddressRepository = customerAddressRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(RemoveCustomerAddressCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RemoveCustomerAddressCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await _customerAddressRepository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null || !entity.IsActive)
                        return Result.Failure(new Error("CustomerAddress.NotFound", "Customer address not found."));

                    await _customerAddressRepository.RemoveAsync(request.Id, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
