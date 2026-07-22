using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Branches.GetByCompany;

public sealed record GetBranchesByCompanyQuery(long CompanyId) : IQuery<IReadOnlyCollection<BranchResponse>>;
