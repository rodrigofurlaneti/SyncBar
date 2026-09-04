using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.CustomerAddresses.Update
{
    internal sealed class UpdateCustomerAddressCommandHandler(
        ICustomerAddressRepository customerAddressRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseCommandHandler<UpdateCustomerAddressCommand>(logRepository, unitOfWork)
    {
        public override async Task<Result> Handle(UpdateCustomerAddressCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateCustomerAddressCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await customerAddressRepository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null || !entity.IsActive)
                        return Result.Failure(new Error("CustomerAddress.NotFound", "Customer address not found."));

                    var updateResult = entity.UpdateDetails(request.Street, request.Number, request.Supplement);
                    if (updateResult.IsFailure)
                        return Result.Failure(updateResult.Error);

                    await customerAddressRepository.UpdateAsync(entity, cancellationToken);
                    await unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
