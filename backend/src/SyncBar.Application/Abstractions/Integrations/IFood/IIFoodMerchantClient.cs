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
}
