namespace SyncBar.Application.Abstractions.Integrations.Ifood;

public sealed record IfoodLogisticsActionResult(bool Success, string? ErrorMessage);

// Success = a chamada HTTP funcionou; CodeMatched só é relevante quando Success é true (o Ifood
// responde 200 com {success: boolean} mesmo quando o código digitado está errado — isso não é
// erro de transporte, é uma tentativa de verificação que falhou).
public sealed record IfoodVerifyDeliveryCodeResult(bool Success, bool CodeMatched, string? ErrorMessage);

// Fase 9c — GET /orders/{id} (detalhes da entrega). A doc oficial documenta a resposta de sucesso
// só como "<object>" (200 OK), sem NENHUM schema de campos — nem nomeado nem por exemplo (ver
// auditoria de 2026-08-20 no projeto claude.ai). Por isso o payload é exposto cru (RawPayload,
// string JSON) em vez de um DTO tipado — tipar campos aqui seria adivinhação, não uma leitura da
// doc. Quem consumir este resultado decide o que extrair do JSON manualmente.
public sealed record IfoodLogisticsOrderDetailsResult(bool Success, string? RawPayload, string? ErrorMessage);

/// <summary>
/// Abstração do módulo Logistics do Ifood (fase 7, entrega por frota própria) — assignDriver,
/// goingToOrigin, arrivedAtOrigin, dispatch, arrivedAtDestination, verifyDeliveryCode. Todos os
/// endpoints ficam sob /logistics/v1.0/orders/{id}, onde {id} é o IfoodOrderId (o mesmo
/// identificador string usado no módulo Order — não confundir com o Id local do SyncBar).
/// Endpoints e formatos confirmados em 2026-08-20 contra a documentação oficial (Postman
/// collection "Logistics") colada pelo usuário. Implementação real:
/// Infrastructure.Integrations.Ifood.IfoodLogisticsClient.
///
/// Fase 9c: GET /orders/{id} (GetOrderDetailsAsync) implementado como payload cru — ver ressalva
/// na DTO acima. Não há necessidade de mais que isso no fluxo essencial porque o SyncBar já guarda
/// localmente o que precisa em IfoodLogisticsDelivery; este endpoint serve como consulta
/// complementar/diagnóstico direto contra o Ifood.
/// </summary>
public interface IIfoodLogisticsClient
{
    Task<IfoodLogisticsActionResult> AssignDriverAsync(
        string accessToken, string IfoodOrderId, string workerName, string workerPhone, string workerVehicleType,
        CancellationToken cancellationToken = default);
    Task<IfoodLogisticsActionResult> GoingToOriginAsync(string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default);
    Task<IfoodLogisticsActionResult> ArrivedAtOriginAsync(string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default);
    Task<IfoodLogisticsActionResult> DispatchAsync(string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default);
    Task<IfoodLogisticsActionResult> ArrivedAtDestinationAsync(string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default);
    Task<IfoodVerifyDeliveryCodeResult> VerifyDeliveryCodeAsync(
        string accessToken, string IfoodOrderId, string code, CancellationToken cancellationToken = default);

    // Fase 9c: consulta opaca — ver ressalva na DTO IfoodLogisticsOrderDetailsResult.
    Task<IfoodLogisticsOrderDetailsResult> GetOrderDetailsAsync(string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default);
}
