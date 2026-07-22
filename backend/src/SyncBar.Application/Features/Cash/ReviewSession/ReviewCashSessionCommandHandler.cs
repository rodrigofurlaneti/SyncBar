using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.ReviewSession;

internal sealed class ReviewCashSessionCommandHandler(
    ICashSessionRepository cashSessionRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReviewCashSessionCommand>
{
    public async Task<Result> Handle(ReviewCashSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await cashSessionRepository.GetByIdForUpdateAsync(request.CashSessionId, cancellationToken);
        if (session is null || !session.IsActive)
            return Result.Failure(new Error("CashSession.NotFound", "Cash session not found."));

        var result = session.MarkAsReviewed();
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
