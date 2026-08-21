using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

internal sealed class MarkIFoodArrivedAtDestinationCommandHandler : BaseCommandHandler<MarkIFoodArrivedAtDestinationCommand>
{
    private readonly IIFoodOrderRepository _ifoodOrderRepository;
    private readonly IIFoodLogisticsDeliveryRepository _deliveryRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIFoodTokenProvider _tokenProvider;
    private readonly IIFoodLogisticsClient _logisticsClient;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public MarkIFoodArrivedAtDestinationCommandHandler(
        IIFoodOrderRepository ifoodOrderRepository,
        IIFoodLogisticsDeliveryRepository deliveryRepository,
        IBranchRepository branchRepository,
        IIFoodTokenProvider tokenProvider,
        IIFoodLogisticsClient logisticsClient,
        TimeProvider timeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _ifoodOrderRepository = ifoodOrderRepository;
        _deliveryRepository = deliveryRepository;
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _logisticsClient = logisticsClient;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(MarkIFoodArrivedAtDestinationCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(MarkIFoodArrivedAtDestinationCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var ifoodOrder = await _ifoodOrderRepository.GetByIdForUpdateAsync(request.IFoodOrderId, cancellationToken);
                if (ifoodOrder is null)
                    return Result.Failure(new Error("IFoodOrder.NotFound", "Pedido iFood não encontrado."));

                var delivery = await _deliveryRepository.GetByIFoodOrderIdForUpdateAsync(ifoodOrder.Id, cancellationToken);
                if (delivery is null)
                    return Result.Failure(new Error("IFoodLogisticsDelivery.NotFound", "Nenhum entregador atribuído a este pedido."));

                var branch = await _branchRepository.GetByIdAsync(ifoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await _tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var actionResult = await _logisticsClient.ArrivedAtDestinationAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                if (!actionResult.Success)
                    return Result.Failure(new Error("IFood.ActionFailed", actionResult.ErrorMessage ?? "Falha ao registrar chegada no destino no iFood."));

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var transition = delivery.MarkArrivedAtDestination(now);
                if (transition.IsFailure)
                    return Result.Failure(transition.Error);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}
