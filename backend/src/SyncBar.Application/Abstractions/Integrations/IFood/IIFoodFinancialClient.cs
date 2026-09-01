namespace SyncBar.Application.Abstractions.Integrations.Ifood;

public sealed record IfoodFinancialEventDto(
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

public sealed record IfoodSettlementDto(
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

// Catálogo dos relatórios "read-only" do Financial que NÃO viraram entidade local (ao contrário
// de Reconciliation/Settlements, que alimentam IfoodFinancialEvent/IfoodSettlement) — expostos
// via um único método genérico (GetReportAsync) que devolve o JSON bruto de cada registro,
// porque a doc oficial (Postman) não documenta o formato de resposta campo-a-campo pra nenhum
// deles (só o path e os query params de filtro). Decisão consciente pra viabilizar 100% de
// cobertura desses 14 endpoints ainda hoje sem inventar um schema tipado não confirmado — ver
// nota na Fase 9 do Ifood-integration-status.md.
public enum IfoodFinancialReportType
{
    // financial/v2.0
    SalesAdjustments,
    Payments,
    PaymentDetails,
    Occurrences,
    MaintenanceFees,
    IncomeTaxes,
    Periods,
    ChargeCancellations,
    Cancellations,
    ReceivableRecords,
    SalesBenefits,
    AdjustmentsBenefits,
    // financial/v2.1
    SalesV21,
    // financial/v3.0
    AnticipationsV3,
    SalesV3,
}

public sealed record IfoodFinancialReportResultDto(IReadOnlyCollection<string> RawItems);

public sealed record IfoodReconciliationOnDemandRequestDto(string RequestId, string RawPayload);

/// <summary>
/// Abstração para o módulo Financial do Ifood. Implementação real:
/// Infrastructure.Integrations.Ifood.IfoodFinancialClient.
///
/// Fase 9 (auditoria de 100% de endpoints): <see cref="GetFinancialEventsAsync"/> deixou de
/// chamar a URL inexistente "financial/v3/financial-events" (bug confirmado — esse path nunca
/// existiu em nenhuma versão oficial da API) e agora chama o endpoint real e equivalente,
/// financial/v3.0/merchants/{merchantId}/reconciliation (filtrado por "competence", não por
/// intervalo de datas — o client itera os meses cobertos pelo período pedido). O nome do método
/// e da entidade de domínio (IfoodFinancialEvent) foram mantidos pra não propagar o rename por
/// todo o handler de sync/summary já homologado; conceitualmente "reconciliation" É o relatório
/// de lançamentos financeiros detalhados por pedido.
///
/// ⚠️ Os nomes de campo dentro do JSON (id/eventId, amount/value, competenceDate, etc.) seguem
/// sendo melhor-esforço — a doc oficial não expõe o schema de resposta campo-a-campo pra este
/// endpoint. RawPayload sempre guarda o JSON bruto pra conferência manual.
/// </summary>
public interface IIfoodFinancialClient
{
    // O Ifood limita a 33 dias por chamada em endpoints por intervalo — respeitado por quem
    // chama, não pelo client. Aqui o intervalo é convertido internamente em 1+ chamadas por
    // competência (yyyy-MM), já que financial/v3.0/reconciliation é filtrado por mês.
    Task<IReadOnlyCollection<IfoodFinancialEventDto>> GetFinancialEventsAsync(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IfoodSettlementDto>> GetSettlementsAsync(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    // financial/v3.0/merchants/{merchantId}/anticipations — sem filtros documentados na doc oficial.
    Task<IfoodFinancialReportResultDto> GetAnticipationsAsync(
        string accessToken, string merchantId, CancellationToken cancellationToken = default);

    // financial/v3.0/merchants/{merchantId}/sales — beginSalesDate/endSalesDate/page.
    Task<IfoodFinancialReportResultDto> GetSalesV3Async(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, int page, CancellationToken cancellationToken = default);

    // POST financial/v3.0/merchants/{merchantId}/reconciliation/on-demand — body { competence }.
    Task<IfoodReconciliationOnDemandRequestDto> RequestReconciliationOnDemandAsync(
        string accessToken, string merchantId, string competence, CancellationToken cancellationToken = default);

    // GET financial/v3.0/merchants/{merchantId}/reconciliation/on-demand/{requestId}.
    Task<string?> GetReconciliationOnDemandStatusAsync(
        string accessToken, string merchantId, string requestId, CancellationToken cancellationToken = default);

    // Catálogo genérico dos 13 relatórios restantes (financial/v2.0 ×12 + financial/v2.1 ×1) —
    // cada IfoodFinancialReportType mapeia pro path e pros nomes de query param reais,
    // confirmados contra a coleção Postman oficial (ver switch na implementação).
    Task<IfoodFinancialReportResultDto> GetReportAsync(
        string accessToken, string merchantId, IfoodFinancialReportType reportType,
        string? periodId, DateTime? rangeStart, DateTime? rangeEnd, CancellationToken cancellationToken = default);
}
