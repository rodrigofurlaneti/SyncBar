namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

public sealed record IfoodFinancialEventItemResponse(
    long Id,
    string Name,
    string? Description,
    decimal Amount,
    bool HasTransferImpact,
    DateTime CompetenceDate,
    string? ReferenceType,
    string? ReferenceId,
    long? LinkedIfoodOrderId);

public sealed record IfoodSettlementItemResponse(
    long Id,
    string Type,
    string? Product,
    decimal Amount,
    string Status,
    DateTime? PaymentDate);

// Resumo do período pra tela "Financeiro" em /integracoes/Ifood — soma de eventos com impacto
// no repasse vs soma dos títulos de repasse do mesmo período, com o alerta de discrepância
// (tolerância de 0,01%, conforme recomendação da doc oficial).
public sealed record IfoodFinancialSummaryResponse(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal TotalFinancialEventsWithTransferImpact,
    decimal TotalSettlements,
    bool HasDiscrepancy,
    decimal DiscrepancyAmount,
    IReadOnlyCollection<IfoodFinancialEventItemResponse> Events,
    IReadOnlyCollection<IfoodSettlementItemResponse> Settlements);
