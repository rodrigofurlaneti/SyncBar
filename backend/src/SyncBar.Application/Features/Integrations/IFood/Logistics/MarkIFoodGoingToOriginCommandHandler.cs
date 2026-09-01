using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

internal sealed class MarkIfoodGoingToOriginCommandHandler : BaseCommandHandler<MarkIfoodGoingToOriginCommand>
{
    private readonly IIfoodOrderRepository _IfoodOrderRepository;
    private readonly IIfoodLogisticsDeliveryRepository _deliveryRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIfoodTokenProvider _tokenProvider;
    private readonly IIfoodLogisticsClient _logisticsClient;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public MarkIfoodGoingToOriginCommandHandler(
        IIfoodOrderRepository IfoodOrderRepository,
        IIfoodLogisticsDeliveryRepository deliveryRepository,
        IBranchRepository branchRepository,
        IIfoodTokenProvider tokenProvider,
        IIfoodLogisticsClient logisticsClient,
        TimeProvider timeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _IfoodOrderRepository = IfoodOrderRepository;
        _deliveryRepository = deliveryRepository;
        _branchRepository = branchRepository;
        _tokenProvider = tokenProvider;
        _logisticsClient = logisticsClient;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(MarkIfoodGoingToOriginCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(MarkIfoodGoingToOriginCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var IfoodOrder = await _IfoodOrderRepository.GetByIdForUpdateAsync(request.IfoodOrderId, cancellationToken);
                if (IfoodOrder is null)
                    return Result.Failure(new Error("IfoodOrder.NotFound", "Pedido Ifood não encontrado."));

                var delivery = await _deliveryRepository.GetByIfoodOrderIdForUpdateAsync(IfoodOrder.Id, cancellationToken);
                if (delivery is null)
                    return Result.Failure(new Error("IfoodLogisticsDelivery.NotFound", "Nenhum entregador atribuído a este pedido."));

                var branch = await _branchRepository.GetByIdAsync(IfoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await _tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var actionResult = await _logisticsClient.GoingToOriginAsync(token, IfoodOrder.IfoodOrderId, cancellationToken);
                if (!actionResult.Success)
                    return Result.Failure(new Error("Ifood.ActionFailed", actionResult.ErrorMessage ?? "Falha ao registrar saída para a origem no Ifood."));

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var transition = delivery.MarkGoingToOrigin(now);
                if (transition.IsFailure)
                    return Result.Failure(transition.Error);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}
