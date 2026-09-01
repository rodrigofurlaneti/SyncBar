using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

// POST financial/v3.0/.../reconciliation/on-demand — Competence no formato "yyyy-MM". Devolve o
// RequestId pra consultar o status depois via GetIfoodReconciliationOnDemandStatusQuery.
public sealed record RequestIfoodReconciliationOnDemandCommand(long BranchId, string Competence)
    : ICommand<IfoodReconciliationOnDemandResponse>;
