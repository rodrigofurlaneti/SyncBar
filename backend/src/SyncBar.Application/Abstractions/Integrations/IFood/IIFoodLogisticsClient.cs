namespace SyncBar.Application.Abstractions.Integrations.IFood;

public sealed record IFoodLogisticsActionResult(bool Success, string? ErrorMessage);

// Success = a chamada HTTP funcionou; CodeMatched só é relevante quando Success é true (o iFood
// responde 200 com {success: boolean} mesmo quando o código digitado está errado — isso não é
// erro de transporte, é uma tentativa de verificação que falhou).
public sealed record IFoodVerifyDeliveryCodeResult(bool Success, bool CodeMatched, string? ErrorMessage);

// Fase 9c — GET /orders/{id} (detalhes da entrega). A doc oficial documenta a resposta de sucesso
// só como "<object>" (200 OK), sem NENHUM schema de campos — nem nomeado nem por exemplo (ver
// auditoria de 2026-08-20 no projeto claude.ai). Por isso o payload é exposto cru (RawPayload,
// string JSON) em vez de um DTO tipado — tipar campos aqui seria adivinhação, não uma leitura da
// doc. Quem consumir este resultado decide o que extrair do JSON manualmente.
public sealed record IFoodLogisticsOrderDetailsResult(bool Success, string? RawPayload, string? ErrorMessage);

/// <summary>
/// Abstração do módulo Logistics do iFood (fase 7, entrega por frota própria) — assignDriver,
/// goingToOrigin, arrivedAtOrigin, dispatch, arrivedAtDestination, verifyDeliveryCode. Todos os
/// endpoints ficam sob /logistics/v1.0/orders/{id}, onde {id} é o IFoodOrderId (o mesmo
/// identificador string usado no módulo Order — não confundir com o Id local do SyncBar).
/// Endpoints e formatos confirmados em 2026-08-20 contra a documentação oficial (Postman
/// collection "Logistics") colada pelo usuário. Implementação real:
/// Infrastructure.Integrations.IFood.IFoodLogisticsClient.
///
/// Fase 9c: GET /orders/{id} (GetOrderDetailsAsync) implementado como payload cru — ver ressalva
/// na DTO acima. Não há necessidade de mais que isso no fluxo essencial porque o SyncBar já guarda
/// localmente o que precisa em IFoodLogisticsDelivery; este endpoint serve como consulta
/// complementar/diagnóstico direto contra o iFood.
/// </summary>
public interface IIFoodLogisticsClient
{
    Task<IFoodLogisticsActionResult> AssignDriverAsync(
        string accessToken, string ifoodOrderId, string workerName, string workerPhone, string workerVehicleType,
        CancellationToken cancellationToken = default);
    Task<IFoodLogisticsActionResult> GoingToOriginAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default);
    Task<IFoodLogisticsActionResult> ArrivedAtOriginAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default);
    Task<IFoodLogisticsActionResult> DispatchAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default);
    Task<IFoodLogisticsActionResult> ArrivedAtDestinationAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default);
    Task<IFoodVerifyDeliveryCodeResult> VerifyDeliveryCodeAsync(
        string accessToken, string ifoodOrderId, string code, CancellationToken cancellationToken = default);

    // Fase 9c: consulta opaca — ver ressalva na DTO IFoodLogisticsOrderDetailsResult.
    Task<IFoodLogisticsOrderDetailsResult> GetOrderDetailsAsync(string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default);
}
