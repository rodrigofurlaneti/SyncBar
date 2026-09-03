using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Shift.OpenShift;

internal sealed class OpenShiftClosingCommandHandler : BaseCommandHandler<OpenShiftClosingCommand, long>
{
    private readonly IShiftClosingRepository _shiftClosingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OpenShiftClosingCommandHandler(
        IShiftClosingRepository shiftClosingRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _shiftClosingRepository = shiftClosingRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<long>> Handle(OpenShiftClosingCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(OpenShiftClosingCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                // Registra o ID do funcionário no log para sabermos quem abriu o turno.
                userIdBox.Value = request.OpenedByEmployeeId;

                // Uma única filial não pode ter dois fechamentos de turno abertos ao mesmo tempo.
                var open = await _shiftClosingRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);
                if (open is not null)
                    return Result.Failure<long>(new Error("ShiftClosing.AlreadyOpen", "This branch already has an open shift closing."));

                var shift = ShiftClosing.Open(request.BranchId, request.OpenedByEmployeeId);
                if (shift.IsFailure)
                    return Result.Failure<long>(shift.Error);

                await _shiftClosingRepository.AddAsync(shift.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(shift.Value.Id);
            });
}
