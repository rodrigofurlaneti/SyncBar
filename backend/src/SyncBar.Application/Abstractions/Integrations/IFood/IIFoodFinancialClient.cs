namespace SyncBar.Application.Abstractions.Integrations.IFood;

public sealed record IFoodFinancialEventDto(
    string Id,
    string Name,
    string? Description,
    string? Trigger,
    decimal Amount,
    bool HasTransferImpact,
    DateTime CompetenceDate,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime? SettlementExpectedDate,
    string? ReferenceType,
    string? ReferenceId,
    string RawPayload);

public sealed record IFoodSettlementDto(
    string Id,
    string Type,
    string? Product,
    decimal Amount,
    string Status,
    DateTime? PaymentDate,
    string? BankCode,
    string? BankAgency,
    string? BankAccount,
    string RawPayload);

/// <summary>
/// Abstração para o módulo Financial do iFood (Fase 4) — só as duas APIs no escopo desta
/// rodada: Financial Events (detalhamento por lançamento) e Settlement (repasse consolidado
/// semanal). Sales, Reconciliation, Reconciliation On-Demand e Anticipation ficam fora de
/// escopo (ver desenho na Fase 4 do projeto). Implementação real:
/// Infrastructure.Integrations.IFood.IFoodFinancialClient.
///
/// ⚠️ Diferente dos módulos Order/Events/Catalog (onde os nomes de campo do JSON foram
/// confirmados direto contra o texto colado da doc oficial), os nomes de campo aqui foram
/// montados a partir do resumo já registrado no projeto (Conceitos financeiros, Referência de
/// campos, API Financial Events, API Settlement) — precisam ser conferidos contra uma resposta
/// real do sandbox (usar o header x-request-homologation: true) antes de considerar testado.
/// </summary>
public interface IIFoodFinancialClient
{
    // O iFood limita a 33 dias por chamada (respeitado por quem chama, não pelo client).
    Task<IReadOnlyCollection<IFoodFinancialEventDto>> GetFinancialEventsAsync(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IFoodSettlementDto>> GetSettlementsAsync(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);
}
