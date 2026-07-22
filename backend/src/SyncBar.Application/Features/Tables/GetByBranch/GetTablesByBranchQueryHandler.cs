using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Tables.GetByBranch;

internal sealed class GetTablesByBranchQueryHandler(IDiningTableRepository diningTableRepository)
    : IQueryHandler<GetTablesByBranchQuery, IReadOnlyCollection<TableResponse>>
{
    public async Task<Result<IReadOnlyCollection<TableResponse>>> Handle(
        GetTablesByBranchQuery request, CancellationToken cancellationToken)
    {
        var tables = await diningTableRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        IReadOnlyCollection<TableResponse> response = tables
            .OrderBy(t => t.Number)
            .Select(t => new TableResponse(t.Id, t.BranchId, t.TableStatusId, t.Number, t.Capacity))
            .ToList();

        return Result.Success(response);
    }
}
