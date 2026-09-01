namespace SyncBar.Application.Abstractions.Integrations.Ifood;

public sealed record IfoodMerchantValidation(string Id, string State, string? Message);

// Fase 13 — campo Available adicionado: a resposta bruta de GET /merchants/{id}/status já traz
// um "available: boolean" por operação (mesmo shape do endpoint por operação, confirmado contra
// a coleção Postman oficial do módulo Merchant), mas até a Fase 13 o client só extraía o campo
// de texto "state"/"status" e descartava o booleano — obrigando quem consumisse o status a
// adivinhar disponibilidade a partir do vocabulário (não documentado) do campo state. Usado pelo
// IfoodMerchantStatusWatcherBackgroundService pra detectar transições disponível↔indisponível de
// forma confiável, sem depender de um texto de estado cujo vocabulário exato não foi confirmado.
public sealed record IfoodMerchantStatusResult(
    bool Success, string? OperationState, bool Available, IReadOnlyCollection<IfoodMerchantValidation> Validations, string? ErrorMessage);

public sealed record IfoodInterruption(string Id, string? Description, DateTime Start, DateTime End);

public sealed record IfoodInterruptionsResult(bool Success, IReadOnlyCollection<IfoodInterruption> Interruptions, string? ErrorMessage);

public sealed record IfoodCreateInterruptionResult(bool Success, string? InterruptionId, string? ErrorMessage);

// DayOfWeek segue a convenção do .NET (0 = domingo .. 6 = sábado) — o client converte pro
// formato de 3 letras que a API do Ifood espera (ver comentário na implementação).
public sealed record IfoodOpeningHourShift(int DayOfWeek, TimeSpan Start, int DurationMinutes);

public sealed record IfoodOpeningHoursResult(bool Success, IReadOnlyCollection<IfoodOpeningHourShift> Shifts, string? ErrorMessage);

public sealed record IfoodMerchantActionResult(bool Success, string? ErrorMessage);

// Fase 9c — List merchants (GET /merchants) e Get merchant details (GET /merchants/{id}).
// Confirmados campo-a-campo contra os exemplos de resposta da própria coleção Postman oficial
// (auditoria de 2026-08-20/21), diferente do restante do client (que usa parsing defensivo por
// falta de exemplo). List merchants NÃO exige MerchantId — é por token/empresa, cobre todas as
// lojas habilitadas pro client_id usado.
public sealed record IfoodMerchantSummaryDto(string Id, string? Name, string? CorporateName);

public sealed record IfoodMerchantListResult(bool Success, IReadOnlyCollection<IfoodMerchantSummaryDto> Merchants, string? ErrorMessage);

public sealed record IfoodMerchantAddressDto(
    string? Country, string? State, string? City, string? PostalCode, string? District,
    string? Street, string? Number, double? Latitude, double? Longitude);

public sealed record IfoodMerchantDetailsResult(
    bool Success,
    string? Id,
    string? Name,
    string? CorporateName,
    string? Description,
    string? Type,
    string? Status,
    DateTime? CreatedAt,
    IfoodMerchantAddressDto? Address,
    string? ErrorMessage);

// Fase 9c — Get merchant status by operation (GET /merchants/{id}/status/{operation}) — status
// "por operação" (ex.: DELIVERY, TAKEOUT), diferente de GetStatusAsync (status geral, primeira
// operação da lista). Reaproveita IfoodMerchantValidation (mesmo shape id/state/message).
public sealed record IfoodMerchantStatusByOperationResult(
    bool Success,
    string? Operation,
    string? SalesChannel,
    bool Available,
    string? State,
    IReadOnlyCollection<IfoodMerchantValidation> Validations,
    string? ErrorMessage);

/// <summary>
/// Cliente HTTP do módulo Merchant do Ifood (Fase 5, "operação da loja") — endpoints e métodos
/// confirmados em 2026-08-19 contra a documentação oficial completa colada pelo usuário
/// (Introdução, Como funciona, Operações, Boas práticas e troubleshooting, Endpoints). Cobre
/// status (leitura), interrupções (criar/listar/remover), horários de funcionamento
/// (ler/substituir) e tempo de preparo (definir/remover). Base URL confirmada:
/// https://merchant-api.Ifood.com.br/merchant/v1.0.
///
/// Diferente do IfoodFinancialClient (Fase 4), os NOMES DE ENDPOINT e os MÉTODOS HTTP aqui vêm
/// da doc completa (mesmo nível de confiança que Catalog/Order/Events). O que NÃO foi validado
/// campo-a-campo contra uma resposta real de sandbox é a forma exata do corpo JSON de cada
/// resposta (nomes de propriedade dentro de "validations", do objeto de interrupção, e do
/// objeto de turno de horário) — o parsing usa múltiplos nomes candidatos e falha de forma
/// graciosa (ver implementação), então um nome errado degrada pra "não achei esse campo" em vez
/// de derrubar a sincronização.
///
/// Tempo de preparo exige o header extra "X-Ifood-Customer-ID" (único conjunto de endpoints do
/// Merchant que pede isso) — o valor vem de IfoodIntegrationSetting.IfoodCustomerId, configurado
/// manualmente pelo usuário na tela de credenciais (fonte exata desse UUID no portal do Ifood
/// ainda não confirmada na prática — ver Pendente no doc de status).
///
/// ⚠️ RISCO CONHECIDO (auditoria de 2026-08-20/21): os endpoints usados por
/// UpsertPreparationTimeAsync/DeletePreparationTimeAsync (PUT|POST|DELETE
/// /merchants/{id}/myPreparationTime) NÃO existem na coleção Postman oficial do módulo Merchant
/// auditada — a coleção completa (9 endpoints) foi enumerada campo-a-campo via jq e "Preparation"
/// não aparece em nenhum nome de endpoint. A implementação foi mantida como estava (nenhuma
/// alternativa oficial foi encontrada pra substituí-la — trocar o path seria adivinhação, não uma
/// correção) mas deve ser tratada como NÃO CONFIÁVEL até verificação manual em sandbox real; ver
/// claude/auditoria-endpoints-Ifood.md no projeto.
///
/// Fase 9c: fecha os gaps restantes do módulo Merchant da auditoria — List merchants
/// (ListMerchantsAsync), Get merchant details (GetMerchantDetailsAsync) e Get merchant status by
/// operation (GetStatusByOperationAsync).
/// </summary>
public interface IIfoodMerchantClient
{
    Task<IfoodMerchantStatusResult> GetStatusAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);

    Task<IfoodInterruptionsResult> GetInterruptionsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);

    Task<IfoodCreateInterruptionResult> CreateInterruptionAsync(
        string accessToken, string merchantId, string description, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    Task<IfoodMerchantActionResult> DeleteInterruptionAsync(
        string accessToken, string merchantId, string interruptionId, CancellationToken cancellationToken = default);

    Task<IfoodOpeningHoursResult> GetOpeningHoursAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);

    // Substitui a lista inteira de turnos — sempre envie todos os turnos ativos da filial, nunca
    // um diff (ver comentário em IfoodOpeningHours).
    Task<IfoodMerchantActionResult> SetOpeningHoursAsync(
        string accessToken, string merchantId, IReadOnlyCollection<IfoodOpeningHourShift> shifts, CancellationToken cancellationToken = default);

    // Tenta PUT (atualizar) e cai pra POST (criar) se o Ifood responder que não existe
    // configuração ainda — evita ter que rastrear localmente se já existe ou não.
    Task<IfoodMerchantActionResult> UpsertPreparationTimeAsync(
        string accessToken, string merchantId, string IfoodCustomerId, int minutes, CancellationToken cancellationToken = default);

    Task<IfoodMerchantActionResult> DeletePreparationTimeAsync(
        string accessToken, string merchantId, string IfoodCustomerId, CancellationToken cancellationToken = default);

    // Fase 9c: List merchants — não recebe merchantId, é por token (todas as lojas do client_id).
    Task<IfoodMerchantListResult> ListMerchantsAsync(string accessToken, int page = 1, int size = 100, CancellationToken cancellationToken = default);

    Task<IfoodMerchantDetailsResult> GetMerchantDetailsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);

    Task<IfoodMerchantStatusByOperationResult> GetStatusByOperationAsync(
        string accessToken, string merchantId, string operation, CancellationToken cancellationToken = default);
}
