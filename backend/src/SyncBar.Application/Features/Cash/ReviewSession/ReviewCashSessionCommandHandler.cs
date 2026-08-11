using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.ReviewSession;

internal sealed class ReviewCashSessionCommandHandler(
    ICashSessionRepository cashSessionRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<ReviewCashSessionCommand>(logRepository, unitOfWork)
{
    public override Task<Result> Handle(ReviewCashSessionCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(ReviewCashSessionCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                // Se o seu request tiver uma propriedade com o ID de quem está revisando (ex: ReviewedByEmployeeId), 
                // descomente e use a linha abaixo para registrar no log:
                // userIdBox.Value = request.ReviewedByEmployeeId;

                var session = await cashSessionRepository.GetByIdForUpdateAsync(request.CashSessionId, cancellationToken);
                if (session is null || !session.IsActive)
                    return Result.Failure(new Error("CashSession.NotFound", "Cash session not found."));

                var result = session.MarkAsReviewed();
                if (result.IsFailure)
                    return result;

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
}