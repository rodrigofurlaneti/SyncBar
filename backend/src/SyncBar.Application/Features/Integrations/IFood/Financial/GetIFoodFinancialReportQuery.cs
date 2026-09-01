using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

// Fase 9 — catálogo genérico dos 13 relatórios financeiros restantes (financial/v2.0 ×12 +
// financial/v2.1 ×1) que não viraram entidade local. PeriodId/RangeStart/RangeEnd são opcionais
// e mapeados pro(s) query param(s) reais de cada ReportType dentro do client (ver
// IfoodFinancialClient.BuildReportUrl).
public sealed record GetIfoodFinancialReportQuery(
    long BranchId,
    IfoodFinancialReportType ReportType,
    string? PeriodId,
    DateTime? RangeStart,
    DateTime? RangeEnd) : IQuery<IfoodFinancialReportResponse>;
