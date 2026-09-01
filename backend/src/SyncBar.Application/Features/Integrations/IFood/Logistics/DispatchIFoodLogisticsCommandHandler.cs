using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

// Nome "Dispatch" (e não "MarkIfoodDispatched") pra não colidir com o DispatchAsync já existente
// no módulo Order (IIfoodOrderClient) — são endpoints diferentes (order/v1.0 vs logistics/v1.0,
// ver comentário em IIfoodLogisticsClient) apesar do nome parecido na doc do Ifood.
//
// Fase 7: além de despachar no módulo Logistics (rastreamento operacional do entregador), este
// handler também chama o dispatch do módulo ORDER (IIfoodOrderClient.DispatchAsync) — esse
// endpoint já existia desde a fase 2 mas nunca tinha sido exposto por nenhum command (ficou
// "não implementado", ver histórico no Ifood-integration-status do projeto claude.ai): é
// exatamente aqui, no primeiro fluxo de frota própria, que ele passa a fazer sentido — marca o
// PEDIDO em si (não só a entrega) como despachado perante o Ifood. Se o dispatch do módulo Order
// falhar depois do de Logistics já ter funcionado, a entrega local ainda avança (o rastreamento
// operacional não deve travar por causa disso) — só o status do IfoodOrder fica desatualizado
// até uma correção manual.
internal sealed class DispatchIfoodLogisticsCommandHandler : BaseCommandHandler<DispatchIfoodLogisticsCommand>
{
    private readonly IIfoodOrderRepository _IfoodOrderRepository;
    private readonly IIfoodLogisticsDeliveryRepository _deliveryRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIfoodTokenProvider _tokenProvider;
    private readonly IIfoodLogisticsClient _logisticsClient;
    private readonly IIfoodOrderClient _orderClient;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public DispatchIfoodLogisticsCommandHandler(
        IIfoodOrderRepository IfoodOrderRepository,
        IIfoodLogisticsDeliveryRepository deliveryRepository,
        IBranchRepository branchRepository,
        IIfoodTokenProvider tokenProvider,
        IIfoodLogisticsClient logisticsClient,
        IIfoodOrderClient orderClient,
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
        _orderClient = orderClient;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(DispatchIfoodLogisticsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DispatchIfoodLogisticsCommandHandler),
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

                var actionResult = await _logisticsClient.DispatchAsync(token, IfoodOrder.IfoodOrderId, cancellationToken);
                if (!actionResult.Success)
                    return Result.Failure(new Error("Ifood.ActionFailed", actionResult.ErrorMessage ?? "Falha ao despachar a entrega no Ifood."));

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var transition = delivery.MarkDispatched(now);
                if (transition.IsFailure)
                    return Result.Failure(transition.Error);

                // Módulo Order: marca o pedido em si como despachado perante o Ifood. Best-effort
                // — se falhar, a entrega (já commitada abaixo) segue seu fluxo normal mesmo assim.
                var orderDispatchResult = await _orderClient.DispatchAsync(token, IfoodOrder.IfoodOrderId, cancellationToken);
                if (orderDispatchResult.Success)
                    IfoodOrder.SetStatus(IfoodOrderStatuses.Dispatched, now);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}
