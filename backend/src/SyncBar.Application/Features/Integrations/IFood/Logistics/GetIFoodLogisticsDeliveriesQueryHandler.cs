using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

internal sealed class GetIFoodLogisticsDeliveriesQueryHandler(
    IIFoodLogisticsDeliveryRepository deliveryRepository,
    IIFoodOrderRepository ifoodOrderRepository,
    ICustomerOrderRepository customerOrderRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodLogisticsDeliveriesQuery, IReadOnlyCollection<IFoodLogisticsDeliveryResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodLogisticsDeliveryResponse>>> Handle(
        GetIFoodLogisticsDeliveriesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodLogisticsDeliveriesQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var deliveries = await deliveryRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);
                if (deliveries.Count == 0)
                    return Result.Success<IReadOnlyCollection<IFoodLogisticsDeliveryResponse>>([]);

                // IFoodLogisticsDelivery.IFoodOrderId aponta pro Id LOCAL do IFoodOrder — busca em
                // lote os pedidos "abertos" da filial pra exibir displayId/cliente/endereço na
                // tela (mesmo padrão de junção usado em GetIFoodOrdersQueryHandler). Um pedido já
                // concluído no lado do iFood mas com entrega ainda não fechada localmente
                // simplesmente não aparece no dicionário — a tela cai pra "Cliente iFood" e sem
                // endereço nesse caso raro, sem quebrar a listagem.
                var ifoodOrders = await ifoodOrderRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);
                var ifoodOrdersById = ifoodOrders.ToDictionary(x => x.Id);

                var customerOrders = await customerOrderRepository.GetByIdsAsync(
                    ifoodOrders.Select(x => x.CustomerOrderId).ToList(), cancellationToken);
                var customerOrdersById = customerOrders.ToDictionary(x => x.Id);

                IReadOnlyCollection<IFoodLogisticsDeliveryResponse> responses = deliveries
                    .Select(d =>
                    {
                        ifoodOrdersById.TryGetValue(d.IFoodOrderId, out var io);
                        Domain.Entities.CustomerOrder? co = null;
                        if (io is not null)
                            customerOrdersById.TryGetValue(io.CustomerOrderId, out co);

                        return new IFoodLogisticsDeliveryResponse(
                            d.Id, d.IFoodOrderId, io?.DisplayId, d.DriverName, d.DriverPhone, d.DriverVehicleType, d.Status,
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
