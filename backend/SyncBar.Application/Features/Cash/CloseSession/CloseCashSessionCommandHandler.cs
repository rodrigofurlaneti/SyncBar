using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.CloseSession;

internal sealed class CloseCashSessionCommandHandler(
    ICashSessionRepository cashSessionRepository,
    ISaleRepository saleRepository,
    ICashMovementRepository cashMovementRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CloseCashSessionCommand, CloseCashSessionResponse>
{
    public async Task<Result<CloseCashSessionResponse>> Handle(CloseCashSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await cashSessionRepository.GetByIdForUpdateAsync(request.CashSessionId, cancellationToken);
        if (session is null || !session.IsActive)
            return Result.Failure<CloseCashSessionResponse>(new Error("CashSession.NotFound", "Cash session not found."));

        var sales = await saleRepository.GetByCashSessionAsync(session.Id, cancellationToken);
        var movements = await cashMovementRepository.GetBySessionAsync(session.Id, cancellationToken);
        var expected = CashMath.ExpectedCash(session.OpeningAmount, sales, movements);

        var result = session.Close(request.ClosedByEmployeeId, request.ClosingAmount, expected);
        if (result.IsFailure)
            return Result.Failure<CloseCashSessionResponse>(result.Error);

        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(new CloseCashSessionResponse(
            session.Id, expected, request.ClosingAmount, session.DifferenceAmount ?? 0));
    }
}
