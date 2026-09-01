using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

// From/To opcionais — null usa os últimos 30 dias (cobre ~4 apurações semanais). Pra tela
// "Financeiro" em /integracoes/Ifood.
public sealed record GetIfoodFinancialSummaryQuery(long BranchId, DateTime? From, DateTime? To)
    : IQuery<IfoodFinancialSummaryResponse>;
