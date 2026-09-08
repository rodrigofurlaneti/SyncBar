using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.GetRegisters;

public sealed record CreateCashRegisterCommand(long BranchId, string Name) : ICommand<long>;
internal sealed class CreateCashRegisterCommandHandler(ICashRegisterRepository registers, IBranchRepository branches,
    ILogTrackerRepository logs, IUnitOfWork unitOfWork) : BaseCommandHandler<CreateCashRegisterCommand, long>(logs, unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public override Task<Result<long>> Handle(CreateCashRegisterCommand request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(CreateCashRegisterCommandHandler), nameof(Handle), null, async _ =>
        {
            var branch = await branches.GetByIdAsync(request.BranchId, ct);
            if (branch is null || !branch.IsActive) return Result.Failure<long>(new Error("Branch.NotFound", "Filial não encontrada."));
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
                return Result.Failure<long>(new Error("CashRegister.InvalidName", "Informe um nome de até 100 caracteres."));
            var register = CashRegister.Create(request.BranchId, request.Name.Trim());
            if (register.IsFailure) return Result.Failure<long>(register.Error);
            await registers.AddAsync(register.Value, ct);
            await _unitOfWork.CommitAsync(ct);
            return Result.Success(register.Value.Id);
        });
}

