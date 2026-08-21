using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Financial;

// From/To opcionais — null usa os últimos 30 dias (cobre ~4 apurações semanais). Pra tela
// "Financeiro" em /integracoes/ifood.
public sealed record GetIFoodFinancialSummaryQuery(long BranchId, DateTime? From, DateTime? To)
    : IQuery<IFoodFinancialSummaryResponse>;
