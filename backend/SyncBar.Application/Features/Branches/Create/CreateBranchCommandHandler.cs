using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Branches.Create;

internal sealed class CreateBranchCommandHandler(IBranchRepository branchRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateBranchCommand, long>
{
    public async Task<Result<long>> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = Branch.Create(
            request.CompanyId, request.Name, request.Cnpj, request.Phone,
            request.AddressStreet, request.AddressNumber, request.AddressDistrict,
            request.AddressCity, request.AddressState, request.AddressZipCode);
        if (branch.IsFailure)
            return Result.Failure<long>(branch.Error);

        await branchRepository.AddAsync(branch.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(branch.Value.Id);
    }
}
