using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Branches.GetByCompany;

internal sealed class GetBranchesByCompanyQueryHandler(IBranchRepository branchRepository)
    : IQueryHandler<GetBranchesByCompanyQuery, IReadOnlyCollection<BranchResponse>>
{
    public async Task<Result<IReadOnlyCollection<BranchResponse>>> Handle(
        GetBranchesByCompanyQuery request, CancellationToken cancellationToken)
    {
        var branches = await branchRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

        IReadOnlyCollection<BranchResponse> response = branches
            .Select(b => new BranchResponse(b.Id, b.Name, b.Cnpj, b.Phone, b.AddressCity, b.AddressState, b.IsActive))
            .ToList();

        return Result.Success(response);
    }
}
