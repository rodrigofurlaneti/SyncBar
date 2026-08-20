using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

// Nome "Dispatch" (e não "MarkIFoodDispatched") pra não colidir com o DispatchAsync já existente
// no módulo Order (IIFoodOrderClient) — são endpoints diferentes (order/v1.0 vs logistics/v1.0,
// ver comentário em IIFoodLogisticsClient) apesar do nome parecido na doc do iFood.
//
// Fase 7: além de despachar no módulo Logistics (rastreamento operacional do entregador), este
// handler também chama o dispatch do módulo ORDER (IIFoodOrderClient.DispatchAsync) — esse
// endpoint já existia desde a fase 2 mas nunca tinha sido exposto por nenhum command (ficou
// "não implementado", ver histórico no ifood-integration-status do projeto claude.ai): é
// exatamente aqui, no primeiro fluxo de frota própria, que ele passa a fazer sentido — marca o
// PEDIDO em si (não só a entrega) como despachado perante o iFood. Se o dispatch do módulo Order
// falhar depois do de Logistics já ter funcionado, a entrega local ainda avança (o rastreamento
// operacional não deve travar por causa disso) — só o status do IFoodOrder fica desatualizado
// até uma correção manual.
internal sealed class DispatchIFoodLogisticsCommandHandler : BaseCommandHandler<DispatchIFoodLogisticsCommand>
{
    private readonly IIFoodOrderRepository _ifoodOrderRepository;
    private readonly IIFoodLogisticsDeliveryRepository _deliveryRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IIFoodTokenProvider _tokenProvider;
    private readonly IIFoodLogisticsClient _logisticsClient;
    private readonly IIFoodOrderClient _orderClient;
    private readonly TimeProvider _timeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public DispatchIFoodLogisticsCommandHandler(
        IIFoodOrderRepository ifoodOrderRepository,
        IIFoodLogisticsDeliveryRepository deliveryRepository,
        IBranchRepository branchRepository,
        IIFoodTokenProvider tokenProvider,
        IIFoodLogisticsClient logisticsClient,
        IIFoodOrderClient orderClient,
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
        _orderClient = orderClient;
        _timeProviderCustom = timeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(DispatchIFoodLogisticsCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DispatchIFoodLogisticsCommandHandler),
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

                var actionResult = await _logisticsClient.DispatchAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                if (!actionResult.Success)
                    return Result.Failure(new Error("IFood.ActionFailed", actionResult.ErrorMessage ?? "Falha ao despachar a entrega no iFood."));

                var now = _timeProviderCustom.GetLocalNow().DateTime;
                var transition = delivery.MarkDispatched(now);
                if (transition.IsFailure)
                    return Result.Failure(transition.Error);

                // Módulo Order: marca o pedido em si como despachado perante o iFood. Best-effort
                // — se falhar, a entrega (já commitada abaixo) segue seu fluxo normal mesmo assim.
                var orderDispatchResult = await _orderClient.DispatchAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                if (orderDispatchResult.Success)
                    ifoodOrder.SetStatus(IFoodOrderStatuses.Dispatched, now);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}
