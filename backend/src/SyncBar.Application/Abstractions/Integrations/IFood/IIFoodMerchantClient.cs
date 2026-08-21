namespace SyncBar.Application.Abstractions.Integrations.IFood;

public sealed record IFoodMerchantValidation(string Id, string State, string? Message);

public sealed record IFoodMerchantStatusResult(
    bool Success, string? OperationState, IReadOnlyCollection<IFoodMerchantValidation> Validations, string? ErrorMessage);

public sealed record IFoodInterruption(string Id, string? Description, DateTime Start, DateTime End);

public sealed record IFoodInterruptionsResult(bool Success, IReadOnlyCollection<IFoodInterruption> Interruptions, string? ErrorMessage);

public sealed record IFoodCreateInterruptionResult(bool Success, string? InterruptionId, string? ErrorMessage);

// DayOfWeek segue a convenção do .NET (0 = domingo .. 6 = sábado) — o client converte pro
// formato de 3 letras que a API do iFood espera (ver comentário na implementação).
public sealed record IFoodOpeningHourShift(int DayOfWeek, TimeSpan Start, int DurationMinutes);

public sealed record IFoodOpeningHoursResult(bool Success, IReadOnlyCollection<IFoodOpeningHourShift> Shifts, string? ErrorMessage);

public sealed record IFoodMerchantActionResult(bool Success, string? ErrorMessage);

// Fase 9c — List merchants (GET /merchants) e Get merchant details (GET /merchants/{id}).
// Confirmados campo-a-campo contra os exemplos de resposta da própria coleção Postman oficial
// (auditoria de 2026-08-20/21), diferente do restante do client (que usa parsing defensivo por
// falta de exemplo). List merchants NÃO exige MerchantId — é por token/empresa, cobre todas as
// lojas habilitadas pro client_id usado.
public sealed record IFoodMerchantSummaryDto(string Id, string? Name, string? CorporateName);

public sealed record IFoodMerchantListResult(bool Success, IReadOnlyCollection<IFoodMerchantSummaryDto> Merchants, string? ErrorMessage);

public sealed record IFoodMerchantAddressDto(
    string? Country, string? State, string? City, string? PostalCode, string? District,
    string? Street, string? Number, double? Latitude, double? Longitude);

public sealed record IFoodMerchantDetailsResult(
    bool Success,
    string? Id,
    string? Name,
    string? CorporateName,
    string? Description,
    string? Type,
    string? Status,
    DateTime? CreatedAt,
    IFoodMerchantAddressDto? Address,
    string? ErrorMessage);

// Fase 9c — Get merchant status by operation (GET /merchants/{id}/status/{operation}) — status
// "por operação" (ex.: DELIVERY, TAKEOUT), diferente de GetStatusAsync (status geral, primeira
// operação da lista). Reaproveita IFoodMerchantValidation (mesmo shape id/state/message).
public sealed record IFoodMerchantStatusByOperationResult(
    bool Success,
    string? Operation,
    string? SalesChannel,
    bool Available,
    string? State,
    IReadOnlyCollection<IFoodMerchantValidation> Validations,
    string? ErrorMessage);

/// <summary>
/// Cliente HTTP do módulo Merchant do iFood (Fase 5, "operação da loja") — endpoints e métodos
/// confirmados em 2026-08-19 contra a documentação oficial completa colada pelo usuário
/// (Introdução, Como funciona, Operações, Boas práticas e troubleshooting, Endpoints). Cobre
/// status (leitura), interrupções (criar/listar/remover), horários de funcionamento
/// (ler/substituir) e tempo de preparo (definir/remover). Base URL confirmada:
/// https://merchant-api.ifood.com.br/merchant/v1.0.
///
/// Diferente do IFoodFinancialClient (Fase 4), os NOMES DE ENDPOINT e os MÉTODOS HTTP aqui vêm
/// da doc completa (mesmo nível de confiança que Catalog/Order/Events). O que NÃO foi validado
/// campo-a-campo contra uma resposta real de sandbox é a forma exata do corpo JSON de cada
/// resposta (nomes de propriedade dentro de "validations", do objeto de interrupção, e do
/// objeto de turno de horário) — o parsing usa múltiplos nomes candidatos e falha de forma
/// graciosa (ver implementação), então um nome errado degrada pra "não achei esse campo" em vez
/// de derrubar a sincronização.
///
/// Tempo de preparo exige o header extra "X-iFood-Customer-ID" (único conjunto de endpoints do
/// Merchant que pede isso) — o valor vem de IFoodIntegrationSetting.IFoodCustomerId, configurado
/// manualmente pelo usuário na tela de credenciais (fonte exata desse UUID no portal do iFood
/// ainda não confirmada na prática — ver Pendente no doc de status).
///
/// ⚠️ RISCO CONHECIDO (auditoria de 2026-08-20/21): os endpoints usados por
/// UpsertPreparationTimeAsync/DeletePreparationTimeAsync (PUT|POST|DELETE
/// /merchants/{id}/myPreparationTime) NÃO existem na coleção Postman oficial do módulo Merchant
/// auditada — a coleção completa (9 endpoints) foi enumerada campo-a-campo via jq e "Preparation"
/// não aparece em nenhum nome de endpoint. A implementação foi mantida como estava (nenhuma
/// alternativa oficial foi encontrada pra substituí-la — trocar o path seria adivinhação, não uma
/// correção) mas deve ser tratada como NÃO CONFIÁVEL até verificação manual em sandbox real; ver
/// claude/auditoria-endpoints-ifood.md no projeto.
///
/// Fase 9c: fecha os gaps restantes do módulo Merchant da auditoria — List merchants
/// (ListMerchantsAsync), Get merchant details (GetMerchantDetailsAsync) e Get merchant status by
/// operation (GetStatusByOperationAsync).
/// </summary>
public interface IIFoodMerchantClient
{
    Task<IFoodMerchantStatusResult> GetStatusAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);

    Task<IFoodInterruptionsResult> GetInterruptionsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);

    Task<IFoodCreateInterruptionResult> CreateInterruptionAsync(
        string accessToken, string merchantId, string description, DateTime start, DateTime end, CancellationToken cancellationToken = default);

    Task<IFoodMerchantActionResult> DeleteInterruptionAsync(
        string accessToken, string merchantId, string interruptionId, CancellationToken cancellationToken = default);

    Task<IFoodOpeningHoursResult> GetOpeningHoursAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);

    // Substitui a lista inteira de turnos — sempre envie todos os turnos ativos da filial, nunca
    // um diff (ver comentário em IFoodOpeningHours).
    Task<IFoodMerchantActionResult> SetOpeningHoursAsync(
        string accessToken, string merchantId, IReadOnlyCollection<IFoodOpeningHourShift> shifts, CancellationToken cancellationToken = default);

    // Tenta PUT (atualizar) e cai pra POST (criar) se o iFood responder que não existe
    // configuração ainda — evita ter que rastrear localmente se já existe ou não.
    Task<IFoodMerchantActionResult> UpsertPreparationTimeAsync(
        string accessToken, string merchantId, string ifoodCustomerId, int minutes, CancellationToken cancellationToken = default);

    Task<IFoodMerchantActionResult> DeletePreparationTimeAsync(
        string accessToken, string merchantId, string ifoodCustomerId, CancellationToken cancellationToken = default);

    // Fase 9c: List merchants — não recebe merchantId, é por token (todas as lojas do client_id).
    Task<IFoodMerchantListResult> ListMerchantsAsync(string accessToken, int page = 1, int size = 100, CancellationToken cancellationToken = default);

    Task<IFoodMerchantDetailsResult> GetMerchantDetailsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);

    Task<IFoodMerchantStatusByOperationResult> GetStatusByOperationAsync(
        string accessToken, string merchantId, string operation, CancellationToken cancellationToken = default);
}
