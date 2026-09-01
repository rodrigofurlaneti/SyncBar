using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

internal sealed class GetIfoodLogisticsDeliveriesQueryHandler(
    IIfoodLogisticsDeliveryRepository deliveryRepository,
    IIfoodOrderRepository IfoodOrderRepository,
    ICustomerOrderRepository customerOrderRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodLogisticsDeliveriesQuery, IReadOnlyCollection<IfoodLogisticsDeliveryResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodLogisticsDeliveryResponse>>> Handle(
        GetIfoodLogisticsDeliveriesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodLogisticsDeliveriesQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var deliveries = await deliveryRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);
                if (deliveries.Count == 0)
                    return Result.Success<IReadOnlyCollection<IfoodLogisticsDeliveryResponse>>([]);

                // IfoodLogisticsDelivery.IfoodOrderId aponta pro Id LOCAL do IfoodOrder — busca em
                // lote os pedidos "abertos" da filial pra exibir displayId/cliente/endereço na
                // tela (mesmo padrão de junção usado em GetIfoodOrdersQueryHandler). Um pedido já
                // concluído no lado do Ifood mas com entrega ainda não fechada localmente
                // simplesmente não aparece no dicionário — a tela cai pra "Cliente Ifood" e sem
                // endereço nesse caso raro, sem quebrar a listagem.
                var IfoodOrders = await IfoodOrderRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);
                var IfoodOrdersById = IfoodOrders.ToDictionary(x => x.Id);

                var customerOrders = await customerOrderRepository.GetByIdsAsync(
                    IfoodOrders.Select(x => x.CustomerOrderId).ToList(), cancellationToken);
                var customerOrdersById = customerOrders.ToDictionary(x => x.Id);

                IReadOnlyCollection<IfoodLogisticsDeliveryResponse> responses = deliveries
                    .Select(d =>
                    {
                        IfoodOrdersById.TryGetValue(d.IfoodOrderId, out var io);
                        Domain.Entities.CustomerOrder? co = null;
                        if (io is not null)
                            customerOrdersById.TryGetValue(io.CustomerOrderId, out co);

                        return new IfoodLogisticsDeliveryResponse(
                            d.Id, d.IfoodOrderId, io?.DisplayId, d.DriverName, d.DriverPhone, d.DriverVehicleType, d.Status,
                            co?.CustomerName, co?.DeliveryAddress,
                            d.AssignedAt, d.GoingToOriginAt, d.ArrivedAtOriginAt, d.DispatchedAt, d.ArrivedAtDestinationAt,
                            d.DeliveryCodeVerifiedAt);
                    })
                    .OrderBy(x => x.AssignedAt)
                    .ToList();

                return Result.Success(responses);
            });
    }
}
