using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Polling
{
    /// <summary>
    /// Lógica de processamento de um único evento de pedido da Keeta (log idempotente + transição
    /// de status no KeetaIntegrationOrder local), compartilhada entre o ciclo de polling
    /// (PollKeetaEventsCommandHandler, que processa em lote) e o webhook push (POST /v1/newEvent,
    /// que processa um evento por chamada) — mesma regra de negócio, duas formas de entrega.
    /// Não comita — quem chama decide o agrupamento de commits.
    /// </summary>
    public interface IKeetaOrderEventProcessor
    {
        Task<bool> ProcessAsync(long companyId, long branchId, KeetaPolledEvent polledEvent, CancellationToken cancellationToken = default);
    }

    internal sealed class KeetaOrderEventProcessor(
        IKeetaIntegrationOrderRepository orderRepository,
        IKeetaIntegrationOrderEventLogRepository eventLogRepository) : IKeetaOrderEventProcessor
    {
        private const string EventCreated = "CREATED";

        public async Task<bool> ProcessAsync(long companyId, long branchId, KeetaPolledEvent polledEvent, CancellationToken cancellationToken = default)
        {
            if (await eventLogRepository.ExistsByEventIdAsync(polledEvent.EventId, cancellationToken))
                return true;

            var logResult = KeetaIntegrationOrderEventLog.Create(
                companyId, branchId, polledEvent.EventId, polledEvent.OrderId,
                polledEvent.EventType, polledEvent.RawJson, polledEvent.CreatedAtUtc);

            if (logResult.IsFailure)
                return false;

            var log = logResult.Value;
            var order = await orderRepository.GetByKeetaOrderIdAsync(polledEvent.OrderId, cancellationToken);

            bool processed;

            if (order is null)
            {
                // CREATED (pedido novo) ou qualquer evento fora de ordem cujo pedido ainda não
                // existe localmente: a criação de KeetaIntegrationOrder a partir de um pedido novo
                // depende de um pipeline de mapeamento de itens/produtos (equivalente ao
                // SyncIfoodOrdersCommandHandler) que ainda não existe para o Keeta — o payload
                // bruto fica preservado no log pra reprocessamento assim que esse pipeline existir.
                log.MarkAsFailed(polledEvent.EventType == EventCreated
                    ? "Pedido novo — pipeline de criação de CustomerOrder a partir do Keeta ainda não implementado. Payload bruto preservado para reprocessamento."
                    : "Nenhum KeetaIntegrationOrder local encontrado para este orderId — evento fora de ordem ou pedido ainda não sincronizado.");
                processed = false;
            }
            else
            {
                ApplyStatusTransition(order, polledEvent.EventType);
                orderRepository.Update(order);
                log.MarkAsProcessed();
                processed = true;
            }

            await eventLogRepository.AddAsync(log, cancellationToken);

            return processed;
        }

        private static void ApplyStatusTransition(KeetaIntegrationOrder order, string eventType)
        {
            switch (eventType)
            {
                case "CONFIRMED":
                    order.MarkAsConfirmed();
                    break;
                case "READY_FOR_PICKUP":
                    order.MarkAsReadyForPickup();
                    break;
                case "DELIVERED":
                case "CONCLUDED":
                    order.MarkAsConcluded();
                    break;
                default:
                    order.ChangeStatus(eventType);
                    break;
            }
        }
    }
}
