using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Comandas.GetByBranch;

internal sealed class GetComandasByBranchQueryHandler(IComandaRepository comandaRepository)
    : IQueryHandler<GetComandasByBranchQuery, IReadOnlyCollection<ComandaResponse>>
{
    public async Task<Result<IReadOnlyCollection<ComandaResponse>>> Handle(
        GetComandasByBranchQuery request, CancellationToken cancellationToken)
    {
        var comandas = await comandaRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        // Ordenacao em C# — codigos numericos primeiro, em ordem.
        IReadOnlyCollection<ComandaResponse> response = comandas
            .OrderBy(c => c.Code.Length)
            .ThenBy(c => c.Code)
            .Select(c => new ComandaResponse(c.Id, c.BranchId, c.ComandaStatusId, c.Code))
            .ToList();

        return Result.Success(response);
    }
}
