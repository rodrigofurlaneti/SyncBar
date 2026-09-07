using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.CustomerAddresses.Create
{
    internal sealed class CreateCustomerAddressCommandHandler : BaseCommandHandler<CreateCustomerAddressCommand, long>
    {
        private readonly ICustomerAddressRepository _customerAddressRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCustomerAddressCommandHandler(
            ICustomerAddressRepository customerAddressRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _customerAddressRepository = customerAddressRepository;
            _unitOfWork = unitOfWork;
        }

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
                        request.Supplement,
                        request.ZipCode ?? string.Empty
                    );

                    if (addressResult.IsFailure)
                        return Result.Failure<long>(addressResult.Error);

                    var address = addressResult.Value;

                    await _customerAddressRepository.AddAsync(address, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success(address.Id);
                });
        }
    }
}
