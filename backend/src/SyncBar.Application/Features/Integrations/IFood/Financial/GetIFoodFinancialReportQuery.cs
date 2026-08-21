using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Financial;

// Fase 9 — catálogo genérico dos 13 relatórios financeiros restantes (financial/v2.0 ×12 +
// financial/v2.1 ×1) que não viraram entidade local. PeriodId/RangeStart/RangeEnd são opcionais
// e mapeados pro(s) query param(s) reais de cada ReportType dentro do client (ver
// IFoodFinancialClient.BuildReportUrl).
public sealed record GetIFoodFinancialReportQuery(
    long BranchId,
    IFoodFinancialReportType ReportType,
    string? PeriodId,
    DateTime? RangeStart,
    DateTime? RangeEnd) : IQuery<IFoodFinancialReportResponse>;
