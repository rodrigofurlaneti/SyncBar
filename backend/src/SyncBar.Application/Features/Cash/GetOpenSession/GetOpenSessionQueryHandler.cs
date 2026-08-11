using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.GetOpenSession;

internal sealed class GetOpenSessionQueryHandler(
    ICashSessionRepository cashSessionRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetOpenSessionQuery, CashSessionResponse>(logRepository, unitOfWork)
{
    public override Task<Result<CashSessionResponse>> Handle(GetOpenSessionQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetOpenSessionQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível na Query
            async (userIdBox) =>
            {
                var session = await cashSessionRepository.GetOpenByCashRegisterAsync(request.CashRegisterId, cancellationToken);
                if (session is null)
                    return Result.Failure<CashSessionResponse>(new Error("CashSession.NotFound", "No open session for this cash register."));

                return Result.Success(new CashSessionResponse(
                    session.Id, session.CashRegisterId, session.CashSessionStatusId,
                    session.OpenedByEmployeeId, session.OpeningAmount, session.OpenedAt));
            });
}