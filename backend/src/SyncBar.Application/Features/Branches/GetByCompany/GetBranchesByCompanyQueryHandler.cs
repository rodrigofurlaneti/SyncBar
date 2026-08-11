using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Branches.GetByCompany;

internal sealed class GetBranchesByCompanyQueryHandler(
    IBranchRepository branchRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetBranchesByCompanyQuery, IReadOnlyCollection<BranchResponse>>(logRepository, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<BranchResponse>>> Handle(
        GetBranchesByCompanyQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetBranchesByCompanyQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se tiver no comando/query
            async (userIdBox) =>
            {
                var branches = await branchRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

                IReadOnlyCollection<BranchResponse> response = branches
                    .Select(b => new BranchResponse(b.Id, b.Name, b.Cnpj, b.Phone, b.AddressCity, b.AddressState, b.IsActive))
                    .ToList();

                return Result.Success(response);
            });
}