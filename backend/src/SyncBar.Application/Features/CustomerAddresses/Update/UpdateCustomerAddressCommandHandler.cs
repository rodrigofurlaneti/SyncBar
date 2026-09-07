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
    internal sealed class UpdateCustomerAddressCommandHandler : BaseCommandHandler<UpdateCustomerAddressCommand>
    {
        private readonly ICustomerAddressRepository _customerAddressRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCustomerAddressCommandHandler(
            ICustomerAddressRepository customerAddressRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _customerAddressRepository = customerAddressRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(UpdateCustomerAddressCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateCustomerAddressCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await _customerAddressRepository.GetByIdAsync(request.Id, cancellationToken);
                    if (entity is null || !entity.IsActive)
                        return Result.Failure(new Error("CustomerAddress.NotFound", "Customer address not found."));

                    var updateResult = entity.UpdateDetails(request.Street, request.Number, request.Supplement, request.ZipCode ?? string.Empty);
                    if (updateResult.IsFailure)
                        return Result.Failure(updateResult.Error);

                    await _customerAddressRepository.UpdateAsync(entity, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
