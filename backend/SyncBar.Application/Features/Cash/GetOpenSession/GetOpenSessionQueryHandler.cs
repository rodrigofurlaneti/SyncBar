using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.GetOpenSession;

internal sealed class GetOpenSessionQueryHandler(ICashSessionRepository cashSessionRepository)
    : IQueryHandler<GetOpenSessionQuery, CashSessionResponse>
{
    public async Task<Result<CashSessionResponse>> Handle(GetOpenSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await cashSessionRepository.GetOpenByCashRegisterAsync(request.CashRegisterId, cancellationToken);
        if (session is null)
            return Result.Failure<CashSessionResponse>(new Error("CashSession.NotFound", "No open session for this cash register."));

        return Result.Success(new CashSessionResponse(
            session.Id, session.CashRegisterId, session.CashSessionStatusId,
            session.OpenedByEmployeeId, session.OpeningAmount, session.OpenedAt));
    }
}
