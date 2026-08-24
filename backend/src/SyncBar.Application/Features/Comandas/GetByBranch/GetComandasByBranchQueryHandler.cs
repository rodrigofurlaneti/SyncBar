using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Comandas.GetByBranch;

internal sealed class GetComandasByBranchQueryHandler(
    IComandaRepository comandaRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetComandasByBranchQuery, IReadOnlyCollection<ComandaResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<ComandaResponse>>> Handle(
        GetComandasByBranchQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetComandasByBranchQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se você tiver essa propriedade na sua query
            async (userIdBox) =>
            {
                // Se houver um UserId no request, você pode associá-lo aqui:

                var comandas = await comandaRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                // Ordenacao em C# — codigos numericos primeiro, em ordem.
                IReadOnlyCollection<ComandaResponse> response = comandas
                    .OrderBy(c => c.Code.Length)
                    .ThenBy(c => c.Code)
                    .Select(c => new ComandaResponse(c.Id, c.BranchId, c.ComandaStatusId, c.Code))
                    .ToList();

                return Result.Success(response);
            });
    }
}