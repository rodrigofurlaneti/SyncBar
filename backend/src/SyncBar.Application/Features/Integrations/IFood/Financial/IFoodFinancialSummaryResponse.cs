namespace SyncBar.Application.Features.Integrations.IFood.Financial;

public sealed record IFoodFinancialEventItemResponse(
    long Id,
    string Name,
    string? Description,
    decimal Amount,
    bool HasTransferImpact,
    DateTime CompetenceDate,
    string? ReferenceType,
    string? ReferenceId,
    long? LinkedIFoodOrderId);

public sealed record IFoodSettlementItemResponse(
    long Id,
    string Type,
    string? Product,
    decimal Amount,
    string Status,
    DateTime? PaymentDate);

// Resumo do período pra tela "Financeiro" em /integracoes/ifood — soma de eventos com impacto
// no repasse vs soma dos títulos de repasse do mesmo período, com o alerta de discrepância
// (tolerância de 0,01%, conforme recomendação da doc oficial).
public sealed record IFoodFinancialSummaryResponse(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal TotalFinancialEventsWithTransferImpact,
    decimal TotalSettlements,
    bool HasDiscrepancy,
    decimal DiscrepancyAmount,
    IReadOnlyCollection<IFoodFinancialEventItemResponse> Events,
    IReadOnlyCollection<IFoodSettlementItemResponse> Settlements);
