using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.CloseSession;

internal sealed class CloseCashSessionCommandHandler(
    ICashSessionRepository cashSessionRepository,
    ISaleRepository saleRepository,
    ICashMovementRepository cashMovementRepository,
    IOrderPartialPaymentRepository partialPaymentRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CloseCashSessionCommand, CloseCashSessionResponse>(logRepository, unitOfWork)
{
    public override Task<Result<CloseCashSessionResponse>> Handle(CloseCashSessionCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(CloseCashSessionCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                // Registra o ID do funcionário no log para sabermos quem fechou o caixa
                userIdBox.Value = request.ClosedByEmployeeId;

                var session = await cashSessionRepository.GetByIdForUpdateAsync(request.CashSessionId, cancellationToken);
                if (session is null || !session.IsActive)
                    return Result.Failure<CloseCashSessionResponse>(new Error("CashSession.NotFound", "Cash session not found."));

                var sales = await saleRepository.GetByCashSessionAsync(session.Id, cancellationToken);
                var movements = await cashMovementRepository.GetBySessionAsync(session.Id, cancellationToken);
                var partials = await partialPaymentRepository.GetByCashSessionAsync(session.Id, cancellationToken);

                // Supondo que CashMath seja uma classe estática do seu domínio/aplicação
                var expected = CashMath.ExpectedCash(session.OpeningAmount, sales, movements, partials);

                var result = session.Close(request.ClosedByEmployeeId, request.ClosingAmount, expected);
                if (result.IsFailure)
                    return Result.Failure<CloseCashSessionResponse>(result.Error);

                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(new CloseCashSessionResponse(
                    session.Id, expected, request.ClosingAmount, session.DifferenceAmount ?? 0));
            });
}