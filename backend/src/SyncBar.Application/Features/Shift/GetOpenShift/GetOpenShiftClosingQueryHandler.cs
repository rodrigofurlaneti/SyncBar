using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Shift.GetOpenShift;

internal sealed class GetOpenShiftClosingQueryHandler(
    IShiftClosingRepository shiftClosingRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetOpenShiftClosingQuery, ShiftClosingResponse>(logRepository, unitOfWork)
{
    public override Task<Result<ShiftClosingResponse>> Handle(GetOpenShiftClosingQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetOpenShiftClosingQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível na Query
            async (userIdBox) =>
            {
                var shift = await shiftClosingRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);
                if (shift is null)
                    return Result.Failure<ShiftClosingResponse>(new Error("ShiftClosing.NotFound", "No open shift for this branch."));

                return Result.Success(new ShiftClosingResponse(
                    shift.Id,
                    shift.BranchId,
                    shift.ShiftClosingStatusId,
                    shift.OpenedByEmployeeId,
                    shift.ClosedByEmployeeId,
                    shift.PeriodStart,
                    shift.PeriodEnd,
                    shift.CashSessionsCount,
                    shift.TotalOpeningAmount,
                    shift.TotalExpectedAmount,
                    shift.TotalRealizedAmount,
                    shift.TotalDifferenceAmount,
                    shift.Notes));
            });
}
