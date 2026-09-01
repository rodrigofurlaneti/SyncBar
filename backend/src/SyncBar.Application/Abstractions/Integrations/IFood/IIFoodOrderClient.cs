namespace SyncBar.Application.Abstractions.Integrations.Ifood;

public sealed record IfoodPollingEvent(string Id, string Code, string? FullCode, string OrderId, DateTime CreatedAt);

// Fase 6a (extensão): opção de complemento selecionada dentro de um item do pedido — Id é o
// option.id do Ifood, casado contra IfoodComplementMapping.IfoodOptionId (ver
// IIfoodComplementMappingRepository.GetByIfoodOptionIdAndBranchAsync). ⚠️ Ainda NÃO confirmado
// campo-a-campo contra uma resposta real de sandbox (o "fluxo essencial" original, fases 2/2.1,
// não cobria pedidos com complementos — mesma ressalva já registrada para o payload de saída em
// IIfoodCatalogClient).
public sealed record IfoodOrderItemOptionDto(string? Id, string? Name, decimal Quantity, decimal UnitPrice);

public sealed record IfoodOrderItemDto(
    string? ExternalCode, string? Ean, string Name, decimal Quantity, decimal UnitPrice,
    IReadOnlyCollection<IfoodOrderItemOptionDto> Options);

public sealed record IfoodOrderDetailsDto(
    string Id,
    string? DisplayId,
    string OrderType,
    string OrderTiming,
    string Category,
    DateTime CreatedAt,
    DateTime? PreparationStartDateTime,
    string MerchantId,
    string? CustomerName,
    string? CustomerPhone,
    string? DeliveryAddressFormatted,
    string? DeliveredBy,
    string? TakeoutMode,
    decimal OrderAmount,
    IReadOnlyCollection<IfoodOrderItemDto> Items);

public sealed record IfoodCancellationReasonDto(string Code, string Description);

public sealed record IfoodOrderActionResult(bool Success, string? ErrorMessage);

// Fase 9b — rastreamento (GET orders/{id}/tracking) e código de retirada (POST
// orders/{id}/validatePickupCode) do módulo Order, confirmados contra a doc oficial (Postman
// collection "Order") colada pelo usuário em 2026-08-20.
public sealed record IfoodOrderTrackingDto(
    double? Latitude, double? Longitude, DateTime? ExpectedDelivery, double? DeliveryEtaEndMinutes, double? PickupEtaStartMinutes);

public sealed record IfoodPickupValidationResult(bool Success, bool CodeMatched, string? ErrorMessage);

// Disputas Handshake (módulo Order, disputes/{disputeId}/accept|reject|alternatives) — aceitas/
// rejeitadas/negociadas por id (não temos ingestão local dos eventos de disputa nesta fase; a
// equipe informa o disputeId que recebe direto no painel/app do Ifood).
public sealed record IfoodDisputeActionResult(bool Success, string? Status, string? ErrorMessage);

// Fase 9c — Virtual Bag (GET orders/{id}/virtual-bag, módulo Grocery/varejo pesado). Resposta
// oficial é um objeto profundamente aninhado (merchant, customer, bag.items[], payment,
// benefit...) sem confirmação campo-a-campo contra uma resposta real de sandbox — só os campos de
// topo mais úteis pra tela são extraídos aqui (parsing defensivo, mesmo padrão do
// IfoodMerchantClient); RawPayload carrega o JSON completo pra quem precisar de mais detalhe.
public sealed record IfoodVirtualBagItemDto(string? UniqueId, string? Name, int Quantity, string? Ean);

public sealed record IfoodVirtualBagResult(
    bool Success,
    string? Id,
    string? ShortCode,
    string? Status,
    DateTime? CreatedAt,
    string? MerchantName,
    string? CustomerName,
    IReadOnlyCollection<IfoodVirtualBagItemDto> Items,
    string? GrossValueAmount,
    string? GrossValueCurrency,
    string? RawPayload,
    string? ErrorMessage);

// Fase 9c — requestDriver/cancelRequestDriver do PRÓPRIO módulo Order (order/v1.0), distintos dos
// endpoints de mesmo nome do módulo Shipping (shipping/v1.0, já implementados em
// IIfoodShippingClient.RequestDriverForOrderAsync/CancelDriverForOrderAsync) — confirmados como
// paths oficiais separados na auditoria de 2026-08-20 (ver claude/auditoria-endpoints-Ifood.md no
// projeto). Sem corpo de resposta (202 Accepted).
//
// Fase 9c — verifyDeliveryCode do módulo Order (order/v1.0/orders/{id}/verifyDeliveryCode),
// distinto do endpoint homônimo do módulo Logistics (logistics/v1.0, já implementado em
// IIfoodLogisticsClient.VerifyDeliveryCodeAsync). Mesmo shape de resposta ({success: bool}) do já
// existente ValidatePickupCodeAsync — reaproveita IfoodPickupValidationResult.
/// <summary>
/// Abstração para o módulo Order/Events do Ifood (polling, detalhes, confirmar, avançar status,
/// cancelar) — endpoints e formatos confirmados em 2026-08-19 contra a documentação oficial
/// (Fundamentos, Guia de implementação, Detalhes de pedido, Eventos de pedido) colada pelo
/// usuário. Implementação real: Infrastructure.Integrations.Ifood.IfoodOrderClient.
///
/// Fase 2.1 (reforço do polling de eventos): path corrigido pro módulo Events
/// (events/v1.0/events:polling), que exige o conjunto de merchants habilitados na chamada
/// (header x-polling-merchants) — ver doc completa do módulo Events.
///
/// Fase 6a (extensão): IfoodOrderItemDto ganhou Options (ver IfoodOrderItemOptionDto) — permite
/// SyncIfoodOrdersCommandHandler reconhecer complementos escolhidos num pedido vindo do Ifood.
///
/// Fase 9c: fecha os gaps restantes do módulo Order identificados na auditoria de 2026-08-20 —
/// virtual bag, proposta de alternativa em disputa, requestDriver/cancelRequestDriver/
/// verifyDeliveryCode do próprio módulo Order (ver ressalvas acima).
/// </summary>
public interface IIfoodOrderClient
{
    // merchantIds: filiais habilitadas da empresa (x-polling-merchants) — o client agrupa em
    // lotes de até 100 por chamada internamente. Lista vazia retorna sem chamar a API.
    Task<IReadOnlyCollection<IfoodPollingEvent>> PollEventsAsync(string accessToken, IReadOnlyCollection<string> merchantIds, CancellationToken cancellationToken = default);
    Task AcknowledgeEventsAsync(string accessToken, IReadOnlyCollection<string> eventIds, CancellationToken cancellationToken = default);
    // Retorna null em 404 (detalhes ainda não disponíveis) — quem chama decide se tenta de novo depois.
    Task<IfoodOrderDetailsDto?> GetOrderDetailsAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IfoodOrderActionResult> ConfirmOrderAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IfoodOrderActionResult> StartPreparationAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IfoodOrderActionResult> ReadyToPickupAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IfoodOrderActionResult> DispatchAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IfoodCancellationReasonDto>> GetCancellationReasonsAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IfoodOrderActionResult> RequestCancellationAsync(string accessToken, string orderId, string reasonCode, CancellationToken cancellationToken = default);

    // Fase 9b: rastreamento do pedido (posição do entregador em pedidos vindos do Ifood) e
    // validação do código de retirada (pedidos TAKEOUT/DINE_IN, ou pickup por entregador).
    Task<IfoodOrderTrackingDto?> GetOrderTrackingAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IfoodPickupValidationResult> ValidatePickupCodeAsync(string accessToken, string orderId, string code, CancellationToken cancellationToken = default);

    // Fase 9b: disputas Handshake — accept/reject por disputeId (ver ressalva na DTO acima).
    Task<IfoodDisputeActionResult> AcceptDisputeAsync(string accessToken, string disputeId, CancellationToken cancellationToken = default);
    Task<IfoodDisputeActionResult> RejectDisputeAsync(string accessToken, string disputeId, string reason, CancellationToken cancellationToken = default);

    // Fase 9c: proposta de alternativa numa disputa Handshake — alternativeType vem do catálogo de
    // alternativas do Ifood (ex.: REFUND_ITEMS, RESCHEDULE); amount/currency só se aplicam a
    // alternativas que envolvem valor (opcional — null quando a alternativa não pede valor).
    Task<IfoodDisputeActionResult> RequestDisputeAlternativeAsync(
        string accessToken, string disputeId, string alternativeId, string alternativeType,
        decimal? amount, string? currency, CancellationToken cancellationToken = default);

    // Fase 9c: virtual bag (Grocery) — detalhe completo da sacola do pedido.
    Task<IfoodVirtualBagResult> GetVirtualBagAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);

    // Fase 9c: requestDriver/cancelRequestDriver/verifyDeliveryCode do próprio módulo Order (ver
    // ressalva acima sobre a distinção com Shipping/Logistics).
    Task<IfoodOrderActionResult> RequestOrderDriverAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IfoodOrderActionResult> CancelOrderRequestDriverAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IfoodPickupValidationResult> VerifyOrderDeliveryCodeAsync(string accessToken, string orderId, string code, CancellationToken cancellationToken = default);
}
