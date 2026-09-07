using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.OrderOrigin.Update
{
    internal sealed class UpdateOrderOriginCommandHandler : BaseCommandHandler<UpdateOrderOriginCommand>
    {
        private readonly IOrderOriginRepository _orderOriginRepository;
        private readonly TimeProvider _timeProvider;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateOrderOriginCommandHandler(
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

        public override async Task<Result> Handle(UpdateOrderOriginCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateOrderOriginCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var orderOrigin = await _orderOriginRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
                    if (orderOrigin is null)
                    {
                        return Result.Failure(new Error("OrderOrigin.NotFound", "Origem de pedido não encontrada."));
                    }

                    var exists = await _orderOriginRepository.ExistsByNameAsync(
                        request.CompanyId, request.BranchId, request.Name, excludeId: request.Id, cancellationToken);

                    if (exists)
                    {
                        return Result.Failure(new Error("OrderOrigin.AlreadyExists", $"Já existe outra origem cadastrada com o nome '{request.Name}'."));
                    }

                    var currentTime = _timeProvider.GetLocalNow().DateTime;

                    // Atualização dos dados na entidade (ajuste o método na entidade se necessário para expor setters ou Update)
                    // Se a entidade tiver métodos de alteração encapsulados, chame-os aqui.

                    _orderOriginRepository.Update(orderOrigin);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
