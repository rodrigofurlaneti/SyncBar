using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Shift.GetById;

internal sealed class GetShiftClosingByIdQueryHandler(
    IShiftClosingRepository shiftClosingRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetShiftClosingByIdQuery, ShiftClosingResponse>(logRepository, unitOfWork)
{
    public override Task<Result<ShiftClosingResponse>> Handle(GetShiftClosingByIdQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetShiftClosingByIdQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível na Query
            async (userIdBox) =>
            {
                var shift = await shiftClosingRepository.GetByIdAsync(request.ShiftClosingId, cancellationToken);
                if (shift is null || !shift.IsActive)
                    return Result.Failure<ShiftClosingResponse>(new Error("ShiftClosing.NotFound", "Shift closing not found."));

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
