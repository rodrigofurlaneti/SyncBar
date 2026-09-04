using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.CustomerAddresses.RegisterOrder
{
    internal sealed class RegisterCustomerAddressOrderCommandHandler(
        ICustomerAddressRepository customerAddressRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseCommandHandler<RegisterCustomerAddressOrderCommand>(logRepository, unitOfWork)
    {
        public override async Task<Result> Handle(RegisterCustomerAddressOrderCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RegisterCustomerAddressOrderCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await customerAddressRepository.GetByIdAsync(request.AddressId, cancellationToken);
                    if (entity is null || !entity.IsActive)
                        return Result.Failure(new Error("CustomerAddress.NotFound", "Customer address not found."));

                    entity.RegisterOrderUsage(request.OrderId);

                    await customerAddressRepository.UpdateAsync(entity, cancellationToken);
                    await unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
