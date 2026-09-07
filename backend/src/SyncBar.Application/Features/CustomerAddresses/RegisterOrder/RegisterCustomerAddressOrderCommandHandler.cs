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
    internal sealed class RegisterCustomerAddressOrderCommandHandler : BaseCommandHandler<RegisterCustomerAddressOrderCommand>
    {
        private readonly ICustomerAddressRepository _customerAddressRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterCustomerAddressOrderCommandHandler(
            ICustomerAddressRepository customerAddressRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _customerAddressRepository = customerAddressRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(RegisterCustomerAddressOrderCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(RegisterCustomerAddressOrderCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var entity = await _customerAddressRepository.GetByIdAsync(request.AddressId, cancellationToken);
                    if (entity is null || !entity.IsActive)
                        return Result.Failure(new Error("CustomerAddress.NotFound", "Customer address not found."));

                    entity.RegisterOrderUsage(request.OrderId);

                    await _customerAddressRepository.UpdateAsync(entity, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
