namespace SyncBar.Application.Abstractions.Integrations.Keeta
{
    /// <summary>
    /// Ações do ciclo de vida do pedido na API "Open Delivery" da Keeta (endpoints hospedados pela
    /// Keeta — nós chamamos). Cada método resolve o access_token válido para a empresa/filial
    /// internamente (via IKeetaAccessTokenProvider) antes de chamar a API.
    /// </summary>
    public interface IKeetaOrderClient
    {
        Task ConfirmOrderAsync(
            long companyId,
            long branchId,
            string keetaOrderId,
            string orderExternalCode,
            DateTime createdAtUtc,
            string? reason = null,
            int? preparationTimeMinutes = null,
            CancellationToken cancellationToken = default);

        Task MarkReadyForPickupAsync(long companyId, long branchId, string keetaOrderId, CancellationToken cancellationToken = default);

        Task DispatchOrderAsync(
            long companyId,
            long branchId,
            string keetaOrderId,
            KeetaDeliveryTrackingEvent? trackingEvent = null,
            CancellationToken cancellationToken = default);

        Task MarkDeliveredAsync(long companyId, long branchId, string keetaOrderId, CancellationToken cancellationToken = default);

        Task SendTrackingUpdateAsync(
            long companyId,
            long branchId,
            string keetaOrderId,
            KeetaDeliveryTrackingEvent trackingEvent,
            CancellationToken cancellationToken = default);

        Task RequestCancellationAsync(
            long companyId,
            long branchId,
            string keetaOrderId,
            string reason,
            string code,
            string mode,
            IReadOnlyList<string>? outOfStockItems = null,
            IReadOnlyList<string>? invalidItems = null,
            CancellationToken cancellationToken = default);

        Task AcceptRefundAsync(long companyId, long branchId, string keetaOrderId, CancellationToken cancellationToken = default);

        Task RejectRefundAsync(
            long companyId,
            long branchId,
            string keetaOrderId,
            string reason,
            string code,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// GET /v1/events:polling — busca eventos pendentes de uma empresa/filial, opcionalmente
        /// filtrando por um subconjunto de merchants (internalMerchantId).
        /// </summary>
        Task<IReadOnlyList<KeetaPolledEvent>> PollEventsAsync(
            long companyId,
            long branchId,
            IReadOnlyList<string>? merchantIds = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// POST /v1/events/acknowledgment — confirma o recebimento de eventos já polled, evitando
        /// que a Keeta os reenvie nos próximos ciclos.
        /// </summary>
        Task AcknowledgeEventsAsync(
            long companyId,
            long branchId,
            IReadOnlyList<KeetaPolledEvent> events,
            CancellationToken cancellationToken = default);
    }
}
