using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;


namespace SyncBar.Application.Features.OrderOrigin.Create
{
    internal sealed class CreateOrderOriginCommandHandler : BaseCommandHandler<CreateOrderOriginCommand, long>
    {
        private readonly IOrderOriginRepository _orderOriginRepository;
        private readonly TimeProvider _timeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public CreateOrderOriginCommandHandler(
            IOrderOriginRepository orderOriginRepository,
            TimeProvider timeProvider,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderOriginRepository = orderOriginRepository;
            _timeProvider = timeProvider;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<long>> Handle(CreateOrderOriginCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateOrderOriginCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var currentTime = _timeProvider.GetLocalNow().DateTime;

                    var exists = await _orderOriginRepository.ExistsByNameAsync(
                        request.CompanyId, request.BranchId, request.Name, excludeId: null, cancellationToken);

                    if (exists)
                    {
                        return Result.Failure<long>(new Error(
                            "OrderOrigin.AlreadyExists",
                            $"Já existe uma origem de pedido cadastrada com o nome '{request.Name}'."));
                    }

                    var orderOriginResult = SyncBar.Domain.Entities.OrderOrigin.Create(
                        request.CompanyId,
                        request.BranchId,
                        request.Name,
                        currentTime);

                    if (orderOriginResult.IsFailure)
                    {
                        return Result.Failure<long>(orderOriginResult.Error);
                    }

                    var orderOrigin = orderOriginResult.Value;

                    await _orderOriginRepository.AddAsync(orderOrigin, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success(orderOrigin.Id);
                });
        }
    }
}
