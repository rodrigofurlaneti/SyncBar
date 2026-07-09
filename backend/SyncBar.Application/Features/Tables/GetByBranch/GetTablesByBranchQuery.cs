using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Tables.GetByBranch;

public sealed record GetTablesByBranchQuery(long BranchId) : IQuery<IReadOnlyCollection<TableResponse>>;
