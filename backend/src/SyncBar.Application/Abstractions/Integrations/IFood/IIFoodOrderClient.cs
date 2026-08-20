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
}
