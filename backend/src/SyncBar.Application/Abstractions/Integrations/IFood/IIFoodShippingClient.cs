namespace SyncBar.Application.Abstractions.Integrations.Ifood;

// Cotação (GET deliveryAvailabilities) — efêmera, não persistida (ver comentário em
// IfoodShippingDelivery). QuoteId expira (ExpirationAt) e é obrigatório no request-driver.
public sealed record IfoodShippingQuoteResult(
    bool Success,
    string? ErrorMessage,
    string? QuoteId,
    decimal GrossValue,
    decimal Discount,
    decimal NetValue,
    double DeliveryTimeMinMinutes,
    double DeliveryTimeMaxMinutes,
    int DistanceMeters,
    DateTime? ExpirationAt);

public sealed record IfoodShippingItemPayload(
    string Name, string? ExternalCode, int Quantity, decimal UnitPrice, decimal Price, decimal TotalPrice);

public sealed record IfoodShippingRequestDriverPayload(
    string CustomerName,
    string CustomerPhoneAreaCode,
    string CustomerPhoneNumber,
    decimal MerchantFee,
    string QuoteId,
    string PostalCode,
    string StreetNumber,
    string StreetName,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    string Country,
    string? Reference,
    double? Latitude,
    double? Longitude,
    IReadOnlyCollection<IfoodShippingItemPayload> Items);

public sealed record IfoodShippingRequestDriverResult(bool Success, string? ErrorMessage, string? DeliveryId, string? TrackingUrl);

public sealed record IfoodShippingActionResult(bool Success, string? ErrorMessage);

public sealed record IfoodShippingTrackingResult(
    bool Success,
    string? ErrorMessage,
    double? Latitude,
    double? Longitude,
    DateTime? ExpectedDelivery,
    double? DeliveryEtaEndMinutes,
    double? PickupEtaStartMinutes);

public sealed record IfoodShippingCancellationReasonDto(string CancelCodeId, string Description);

public sealed record IfoodSafeDeliveryScoreResult(bool Success, string? ErrorMessage, string? Score);

// Payload do fluxo de troca de endereço em andamento (fase 11) — o cliente pede pra mudar o
// endereço de entrega pelo app dele durante a corrida; o Ifood notifica o lojista (via polling,
// fora do escopo desta abstração) e o lojista usa RequestDeliveryAddressChangeAsync pra propor um
// novo endereço, ou Accept/Deny quando é o CLIENTE quem propôs (fluxo bidirecional — a doc oficial
// não distingue quem inicia, só os 4 verbos de ação). Coordinates são opcionais na doc oficial.
public sealed record IfoodShippingDeliveryAddressChangePayload(
    string StreetNumber,
    string StreetName,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    string Country,
    string? Reference,
    double? Latitude,
    double? Longitude);

/// <summary>
/// Abstração do módulo Shipping do Ifood (fase 8) — "The Shipping API allows the merchant to
/// send orders to Ifood that were placed through other sales channels (phone, whatsapp or their
/// own app or website)": ao contrário de todo módulo anterior (Order/Logistics/Catalog/...), este
/// NÃO opera sobre um IfoodOrderId — o pedido nunca existiu no Ifood; o SyncBar só pede pro Ifood
/// entregar um pacote (endereço + itens informados na hora) usando a malha de entregadores dele.
/// Endpoints e formatos confirmados em 2026-08-20 contra a documentação oficial (Postman
/// collection "Shipping") colada pelo usuário. Implementação real:
/// Infrastructure.Integrations.Ifood.IfoodShippingClient.
///
/// Fase 11 — fecha os últimos 4 endpoints da auditoria (troca de endereço de entrega em
/// andamento): RequestDeliveryAddressChangeAsync/AcceptDeliveryAddressChangeAsync/
/// DenyDeliveryAddressChangeAsync/ConfirmUserAddressAsync, todos sobre a variante "pedido já
/// existente no Ifood" (mesmo IfoodOrderId usado em GetDeliveryAvailabilitiesForOrderAsync).
/// </summary>
public interface IIfoodShippingClient
{
    Task<IfoodShippingQuoteResult> GetDeliveryAvailabilitiesAsync(
        string accessToken, string merchantId, double latitude, double longitude, CancellationToken cancellationToken = default);

    Task<IfoodShippingRequestDriverResult> RequestDriverAsync(
        string accessToken, string merchantId, IfoodShippingRequestDriverPayload payload, CancellationToken cancellationToken = default);

    Task<IfoodShippingTrackingResult> GetTrackingAsync(string accessToken, string deliveryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IfoodShippingCancellationReasonDto>> GetCancellationReasonsAsync(
        string accessToken, string deliveryId, CancellationToken cancellationToken = default);

    Task<IfoodShippingActionResult> CancelAsync(
        string accessToken, string deliveryId, string reason, int cancellationCode, CancellationToken cancellationToken = default);

    Task<IfoodSafeDeliveryScoreResult> GetSafeDeliveryScoreAsync(string accessToken, string deliveryId, CancellationToken cancellationToken = default);

    // Variante "pedido já existente no Ifood" — mesmo módulo Shipping (base /shipping/v1.0, NÃO
    // /order/v1.0, apesar do nome parecido com o requestDriver do módulo Order), mas atua sobre o
    // IfoodOrderId em vez de criar uma entrega nova do zero. Fecha a lacuna de "entrega sob
    // demanda" que o módulo Order nunca teve endpoint próprio implementado pra cobrir.
    Task<IfoodShippingQuoteResult> GetDeliveryAvailabilitiesForOrderAsync(
        string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default);

    Task<IfoodShippingActionResult> RequestDriverForOrderAsync(
        string accessToken, string IfoodOrderId, string quoteId, CancellationToken cancellationToken = default);

    Task<IfoodShippingActionResult> CancelDriverForOrderAsync(string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default);

    // Fase 11 — fluxo de troca de endereço de entrega em andamento.
    Task<IfoodShippingActionResult> RequestDeliveryAddressChangeAsync(
        string accessToken, string IfoodOrderId, IfoodShippingDeliveryAddressChangePayload payload, CancellationToken cancellationToken = default);

    Task<IfoodShippingActionResult> AcceptDeliveryAddressChangeAsync(string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default);

    Task<IfoodShippingActionResult> DenyDeliveryAddressChangeAsync(string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default);

    Task<IfoodShippingActionResult> ConfirmUserAddressAsync(string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default);
}
