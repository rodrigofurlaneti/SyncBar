namespace SyncBar.Application.Abstractions.Integrations.IFood;

public sealed record IFoodPollingEvent(string Id, string Code, string? FullCode, string OrderId, DateTime CreatedAt);

// Fase 6a (extensão): opção de complemento selecionada dentro de um item do pedido — Id é o
// option.id do iFood, casado contra IFoodComplementMapping.IFoodOptionId (ver
// IIFoodComplementMappingRepository.GetByIFoodOptionIdAndBranchAsync). ⚠️ Ainda NÃO confirmado
// campo-a-campo contra uma resposta real de sandbox (o "fluxo essencial" original, fases 2/2.1,
// não cobria pedidos com complementos — mesma ressalva já registrada para o payload de saída em
// IIFoodCatalogClient).
public sealed record IFoodOrderItemOptionDto(string? Id, string? Name, decimal Quantity, decimal UnitPrice);

public sealed record IFoodOrderItemDto(
    string? ExternalCode, string? Ean, string Name, decimal Quantity, decimal UnitPrice,
    IReadOnlyCollection<IFoodOrderItemOptionDto> Options);

public sealed record IFoodOrderDetailsDto(
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
    IReadOnlyCollection<IFoodOrderItemDto> Items);

public sealed record IFoodCancellationReasonDto(string Code, string Description);

public sealed record IFoodOrderActionResult(bool Success, string? ErrorMessage);

// Fase 9b — rastreamento (GET orders/{id}/tracking) e código de retirada (POST
// orders/{id}/validatePickupCode) do módulo Order, confirmados contra a doc oficial (Postman
// collection "Order") colada pelo usuário em 2026-08-20.
public sealed record IFoodOrderTrackingDto(
    double? Latitude, double? Longitude, DateTime? ExpectedDelivery, double? DeliveryEtaEndMinutes, double? PickupEtaStartMinutes);

public sealed record IFoodPickupValidationResult(bool Success, bool CodeMatched, string? ErrorMessage);

// Disputas Handshake (módulo Order, disputes/{disputeId}/accept|reject|alternatives) — aceitas/
// rejeitadas/negociadas por id (não temos ingestão local dos eventos de disputa nesta fase; a
// equipe informa o disputeId que recebe direto no painel/app do iFood).
public sealed record IFoodDisputeActionResult(bool Success, string? Status, string? ErrorMessage);

// Fase 9c — Virtual Bag (GET orders/{id}/virtual-bag, módulo Grocery/varejo pesado). Resposta
// oficial é um objeto profundamente aninhado (merchant, customer, bag.items[], payment,
// benefit...) sem confirmação campo-a-campo contra uma resposta real de sandbox — só os campos de
// topo mais úteis pra tela são extraídos aqui (parsing defensivo, mesmo padrão do
// IFoodMerchantClient); RawPayload carrega o JSON completo pra quem precisar de mais detalhe.
public sealed record IFoodVirtualBagItemDto(string? UniqueId, string? Name, int Quantity, string? Ean);

public sealed record IFoodVirtualBagResult(
    bool Success,
    string? Id,
    string? ShortCode,
    string? Status,
    DateTime? CreatedAt,
    string? MerchantName,
    string? CustomerName,
    IReadOnlyCollection<IFoodVirtualBagItemDto> Items,
    string? GrossValueAmount,
    string? GrossValueCurrency,
    string? RawPayload,
    string? ErrorMessage);

// Fase 9c — requestDriver/cancelRequestDriver do PRÓPRIO módulo Order (order/v1.0), distintos dos
// endpoints de mesmo nome do módulo Shipping (shipping/v1.0, já implementados em
// IIFoodShippingClient.RequestDriverForOrderAsync/CancelDriverForOrderAsync) — confirmados como
// paths oficiais separados na auditoria de 2026-08-20 (ver claude/auditoria-endpoints-ifood.md no
// projeto). Sem corpo de resposta (202 Accepted).
//
// Fase 9c — verifyDeliveryCode do módulo Order (order/v1.0/orders/{id}/verifyDeliveryCode),
// distinto do endpoint homônimo do módulo Logistics (logistics/v1.0, já implementado em
// IIFoodLogisticsClient.VerifyDeliveryCodeAsync). Mesmo shape de resposta ({success: bool}) do já
// existente ValidatePickupCodeAsync — reaproveita IFoodPickupValidationResult.
/// <summary>
/// Abstração para o módulo Order/Events do iFood (polling, detalhes, confirmar, avançar status,
/// cancelar) — endpoints e formatos confirmados em 2026-08-19 contra a documentação oficial
/// (Fundamentos, Guia de implementação, Detalhes de pedido, Eventos de pedido) colada pelo
/// usuário. Implementação real: Infrastructure.Integrations.IFood.IFoodOrderClient.
///
/// Fase 2.1 (reforço do polling de eventos): path corrigido pro módulo Events
/// (events/v1.0/events:polling), que exige o conjunto de merchants habilitados na chamada
/// (header x-polling-merchants) — ver doc completa do módulo Events.
///
/// Fase 6a (extensão): IFoodOrderItemDto ganhou Options (ver IFoodOrderItemOptionDto) — permite
/// SyncIFoodOrdersCommandHandler reconhecer complementos escolhidos num pedido vindo do iFood.
///
/// Fase 9c: fecha os gaps restantes do módulo Order identificados na auditoria de 2026-08-20 —
/// virtual bag, proposta de alternativa em disputa, requestDriver/cancelRequestDriver/
/// verifyDeliveryCode do próprio módulo Order (ver ressalvas acima).
/// </summary>
public interface IIFoodOrderClient
{
    // merchantIds: filiais habilitadas da empresa (x-polling-merchants) — o client agrupa em
    // lotes de até 100 por chamada internamente. Lista vazia retorna sem chamar a API.
    Task<IReadOnlyCollection<IFoodPollingEvent>> PollEventsAsync(string accessToken, IReadOnlyCollection<string> merchantIds, CancellationToken cancellationToken = default);
    Task AcknowledgeEventsAsync(string accessToken, IReadOnlyCollection<string> eventIds, CancellationToken cancellationToken = default);
    // Retorna null em 404 (detalhes ainda não disponíveis) — quem chama decide se tenta de novo depois.
    Task<IFoodOrderDetailsDto?> GetOrderDetailsAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IFoodOrderActionResult> ConfirmOrderAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IFoodOrderActionResult> StartPreparationAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IFoodOrderActionResult> ReadyToPickupAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IFoodOrderActionResult> DispatchAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IFoodCancellationReasonDto>> GetCancellationReasonsAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IFoodOrderActionResult> RequestCancellationAsync(string accessToken, string orderId, string reasonCode, CancellationToken cancellationToken = default);

    // Fase 9b: rastreamento do pedido (posição do entregador em pedidos vindos do iFood) e
    // validação do código de retirada (pedidos TAKEOUT/DINE_IN, ou pickup por entregador).
    Task<IFoodOrderTrackingDto?> GetOrderTrackingAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IFoodPickupValidationResult> ValidatePickupCodeAsync(string accessToken, string orderId, string code, CancellationToken cancellationToken = default);

    // Fase 9b: disputas Handshake — accept/reject por disputeId (ver ressalva na DTO acima).
    Task<IFoodDisputeActionResult> AcceptDisputeAsync(string accessToken, string disputeId, CancellationToken cancellationToken = default);
    Task<IFoodDisputeActionResult> RejectDisputeAsync(string accessToken, string disputeId, string reason, CancellationToken cancellationToken = default);

    // Fase 9c: proposta de alternativa numa disputa Handshake — alternativeType vem do catálogo de
    // alternativas do iFood (ex.: REFUND_ITEMS, RESCHEDULE); amount/currency só se aplicam a
    // alternativas que envolvem valor (opcional — null quando a alternativa não pede valor).
    Task<IFoodDisputeActionResult> RequestDisputeAlternativeAsync(
        string accessToken, string disputeId, string alternativeId, string alternativeType,
        decimal? amount, string? currency, CancellationToken cancellationToken = default);

    // Fase 9c: virtual bag (Grocery) — detalhe completo da sacola do pedido.
    Task<IFoodVirtualBagResult> GetVirtualBagAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);

    // Fase 9c: requestDriver/cancelRequestDriver/verifyDeliveryCode do próprio módulo Order (ver
    // ressalva acima sobre a distinção com Shipping/Logistics).
    Task<IFoodOrderActionResult> RequestOrderDriverAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IFoodOrderActionResult> CancelOrderRequestDriverAsync(string accessToken, string orderId, CancellationToken cancellationToken = default);
    Task<IFoodPickupValidationResult> VerifyOrderDeliveryCodeAsync(string accessToken, string orderId, string code, CancellationToken cancellationToken = default);
}
