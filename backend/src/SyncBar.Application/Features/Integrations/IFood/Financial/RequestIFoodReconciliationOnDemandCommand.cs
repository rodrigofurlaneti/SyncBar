using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Financial;

// POST financial/v3.0/.../reconciliation/on-demand — Competence no formato "yyyy-MM". Devolve o
// RequestId pra consultar o status depois via GetIFoodReconciliationOnDemandStatusQuery.
public sealed record RequestIFoodReconciliationOnDemandCommand(long BranchId, string Competence)
    : ICommand<IFoodReconciliationOnDemandResponse>;
