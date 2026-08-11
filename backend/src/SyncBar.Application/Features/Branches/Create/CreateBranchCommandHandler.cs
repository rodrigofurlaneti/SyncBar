using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Branches.Create;

internal sealed class CreateBranchCommandHandler(
    IBranchRepository branchRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CreateBranchCommand, long>(logRepository, unitOfWork)
{
    public override Task<Result<long>> Handle(CreateBranchCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(CreateBranchCommandHandler),
            nameof(Handle),
            null, 
            async (userIdBox) =>
            {
                // Dica: Se você tiver o ID do usuário logado via request ou interface (UserContext), 
                // você pode atribuí-lo aqui: userIdBox.Value = request.UserId;

                var branch = Branch.Create(
                    request.CompanyId, request.Name, request.Cnpj, request.Phone,
                    request.AddressStreet, request.AddressNumber, request.AddressDistrict,
                    request.AddressCity, request.AddressState, request.AddressZipCode);

                if (branch.IsFailure)
                    return Result.Failure<long>(branch.Error);

                await branchRepository.AddAsync(branch.Value, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(branch.Value.Id);
            });
}