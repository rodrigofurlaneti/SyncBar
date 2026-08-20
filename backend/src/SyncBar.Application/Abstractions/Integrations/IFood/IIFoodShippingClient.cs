namespace SyncBar.Application.Abstractions.Integrations.IFood;

// Cotação (GET deliveryAvailabilities) — efêmera, não persistida (ver comentário em
// IFoodShippingDelivery). QuoteId expira (ExpirationAt) e é obrigatório no request-driver.
public sealed record IFoodShippingQuoteResult(
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

public sealed record IFoodShippingItemPayload(
    string Name, string? ExternalCode, int Quantity, decimal UnitPrice, decimal Price, decimal TotalPrice);

public sealed record IFoodShippingRequestDriverPayload(
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
    IReadOnlyCollection<IFoodShippingItemPayload> Items);

public sealed record IFoodShippingRequestDriverResult(bool Success, string? ErrorMessage, string? DeliveryId, string? TrackingUrl);

public sealed record IFoodShippingActionResult(bool Success, string? ErrorMessage);

public sealed record IFoodShippingTrackingResult(
    bool Success,
    string? ErrorMessage,
    double? Latitude,
    double? Longitude,
    DateTime? ExpectedDelivery,
    double? DeliveryEtaEndMinutes,
    double? PickupEtaStartMinutes);

public sealed record IFoodShippingCancellationReasonDto(string CancelCodeId, string Description);

public sealed record IFoodSafeDeliveryScoreResult(bool Success, string? ErrorMessage, string? Score);

/// <summary>
/// Abstração do módulo Shipping do iFood (fase 8) — "The Shipping API allows the merchant to
/// send orders to iFood that were placed through other sales channels (phone, whatsapp or their
/// own app or website)": ao contrário de todo módulo anterior (Order/Logistics/Catalog/...), este
/// NÃO opera sobre um IFoodOrderId — o pedido nunca existiu no iFood; o SyncBar só pede pro iFood
/// entregar um pacote (endereço + itens informados na hora) usando a malha de entregadores dele.
/// Endpoints e formatos confirmados em 2026-08-20 contra a documentação oficial (Postman
/// collection "Shipping") colada pelo usuário. Implementação real:
/// Infrastructure.Integrations.IFood.IFoodShippingClient.
///
/// NÃO implementado nesta fase (fluxo de troca de endereço em andamento, 4 endpoints:
/// deliveryAddressChangeRequest/acceptDeliveryAddressChange/denyDeliveryAddressChange/
/// userConfirmAddress) — é um fluxo de negociação disparado normalmente pelo APP DO CLIENTE
/// durante a entrega (o cliente pede pra mudar o endereço e o app do entregador manda a
/// notificação), sem um gatilho claro do lado do lojista na doc consultada; fica como próximo
/// incremento quando houver clareza de como o SyncBar ficaria sabendo que uma mudança foi pedida
/// (provavelmente via evento de polling, ainda não mapeado pra este módulo).
/// </summary>
public interface IIFoodShippingClient
{
    Task<IFoodShippingQuoteResult> GetDeliveryAvailabilitiesAsync(
        string accessToken, string merchantId, double latitude, double longitude, CancellationToken cancellationToken = default);

    Task<IFoodShippingRequestDriverResult> RequestDriverAsync(
        string accessToken, string merchantId, IFoodShippingRequestDriverPayload payload, CancellationToken cancellationToken = default);

    Task<IFoodShippingTrackingResult> GetTrackingAsync(string accessToken, string deliveryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IFoodShippingCancellationReasonDto>> GetCancellationReasonsAsync(
        string accessToken, string deliveryId, CancellationToken cancellationToken = default);

    Task<IFoodShippingActionResult> CancelAsync(
        string accessToken, string deliveryId, string reason, int cancellationCode, CancellationToken cancellationToken = default);

    Task<IFoodSafeDeliveryScoreResult> GetSafeDeliveryScoreAsync(string accessToken, string deliveryId, CancellationToken cancellationToken = default);

    // Variante "pedido já existente no iFood" — mesmo módulo Shipping (base /shipping/v1.0, NÃO
    // /order/v1.0, apesar do nome parecido com o requestDriver do módulo Order), mas atua sobre o
    // IFoodOrderId em vez de criar uma entrega nova do zero. Fecha a lacuna de "entrega sob
    // demanda" que o módulo Order nunca teve endpoint próprio implementado pra cobrir.
    Task<IFoodShippingQuoteResult> GetDeliveryAvailabilitiesForOrderAsync(
        string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default);

    Task<IFoodShippingActionResult> RequestDriverForOrderAsync(
        string accessToken, string ifoodOrderId, string quoteId, CancellationToken cancellationToken = default);

    Task<IFoodShippingActionResult> CancelDriverForOrderAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default);
}
