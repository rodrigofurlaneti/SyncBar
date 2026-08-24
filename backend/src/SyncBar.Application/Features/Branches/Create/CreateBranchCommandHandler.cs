using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Branches.Create;

internal sealed class CreateBranchCommandHandler : BaseCommandHandler<CreateBranchCommand, long>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBranchCommandHandler(
        IBranchRepository branchRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<long>> Handle(CreateBranchCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(CreateBranchCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var branch = Branch.Create(
                    request.CompanyId, request.Name, request.Cnpj, request.Phone,
                    request.AddressStreet, request.AddressNumber, request.AddressDistrict,
                    request.AddressCity, request.AddressState, request.AddressZipCode);

                if (branch.IsFailure)
                    return Result.Failure<long>(branch.Error);

                await _branchRepository.AddAsync(branch.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(branch.Value.Id);
            });
}