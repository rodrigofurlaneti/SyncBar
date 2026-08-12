using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Tables.GetByBranch;

internal sealed class GetTablesByBranchQueryHandler(
    IDiningTableRepository diningTableRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetTablesByBranchQuery, IReadOnlyCollection<TableResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<TableResponse>>> Handle(
        GetTablesByBranchQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetTablesByBranchQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/sistema consultando, preencha:
                // userIdBox.Value = request.UserId;

                var tables = await diningTableRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                IReadOnlyCollection<TableResponse> response = tables
                    .OrderBy(t => t.Number)
                    .Select(t => new TableResponse(t.Id, t.BranchId, t.TableStatusId, t.Number, t.Capacity))
                    .ToList();

                return Result.Success(response);
            });
    }
}