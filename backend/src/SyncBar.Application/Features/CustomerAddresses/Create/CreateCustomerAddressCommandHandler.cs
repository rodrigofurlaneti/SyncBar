using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.CustomerAddresses.Create
{
    internal sealed class CreateCustomerAddressCommandHandler(
        ICustomerAddressRepository customerAddressRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseCommandHandler<CreateCustomerAddressCommand, long>(logRepository, unitOfWork)
    {
        public override async Task<Result<long>> Handle(CreateCustomerAddressCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateCustomerAddressCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var addressResult = SyncBar.Domain.Entities.CustomerAddress.Create(
                        request.CompanyId,
                        request.BranchId,
                        request.CustomerId,
                        request.Street,
                        request.Number,
                        request.Supplement
                    );

                    if (addressResult.IsFailure)
                        return Result.Failure<long>(addressResult.Error);

                    var address = addressResult.Value;

                    await customerAddressRepository.AddAsync(address, cancellationToken);
                    await unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success(address.Id);
                });
        }
    }
}
