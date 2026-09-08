using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Shift.GetHistory;

internal sealed class GetShiftClosingHistoryQueryHandler(
    IShiftClosingRepository shiftClosingRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetShiftClosingHistoryQuery, IReadOnlyCollection<ShiftClosingResponse>>(logRepository, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<ShiftClosingResponse>>> Handle(
        GetShiftClosingHistoryQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetShiftClosingHistoryQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se houver esse campo na Query
            async (userIdBox) =>
            {
                if (request.ReferenceMonth is < 1 or > 12)
                    return Result.Failure<IReadOnlyCollection<ShiftClosingResponse>>(
                        new Error("ShiftClosingHistory.InvalidMonth", "Reference month must be between 1 and 12."));

                var from = new DateTime(request.ReferenceYear, request.ReferenceMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                var to = from.AddMonths(1);

                var shifts = await shiftClosingRepository.GetHistoryByBranchAsync(request.BranchId, from, to, cancellationToken);

                var history = shifts
                    .OrderByDescending(s => s.PeriodStart)
                    .Select(shift => new ShiftClosingResponse(
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
                        shift.Notes))
                    .ToList();

                return Result.Success<IReadOnlyCollection<ShiftClosingResponse>>(history);
            });
}
