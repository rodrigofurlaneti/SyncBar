using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.CloseSession;

internal sealed class CloseCashSessionCommandHandler : BaseCommandHandler<CloseCashSessionCommand, CloseCashSessionResponse>
{
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly ICashMovementRepository _cashMovementRepository;
    private readonly IOrderPartialPaymentRepository _partialPaymentRepository;
    private readonly ICashSessionPaymentReconciliationRepository _paymentReconciliationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseCashSessionCommandHandler(
        ICashSessionRepository cashSessionRepository,
        ISaleRepository saleRepository,
        ICashMovementRepository cashMovementRepository,
        IOrderPartialPaymentRepository partialPaymentRepository,
        ICashSessionPaymentReconciliationRepository paymentReconciliationRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _cashSessionRepository = cashSessionRepository;
        _saleRepository = saleRepository;
        _cashMovementRepository = cashMovementRepository;
        _partialPaymentRepository = partialPaymentRepository;
        _paymentReconciliationRepository = paymentReconciliationRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<CloseCashSessionResponse>> Handle(CloseCashSessionCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(CloseCashSessionCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                // Registra o ID do funcionário no log para sabermos quem fechou o caixa
                userIdBox.Value = request.ClosedByEmployeeId;

                var session = await _cashSessionRepository.GetByIdForUpdateAsync(request.CashSessionId, cancellationToken);
                if (session is null || !session.IsActive)
                    return Result.Failure<CloseCashSessionResponse>(new Error("CashSession.NotFound", "Cash session not found."));

                var sales = await _saleRepository.GetByCashSessionAsync(session.Id, cancellationToken);
                var movements = await _cashMovementRepository.GetBySessionAsync(session.Id, cancellationToken);
                var partials = await _partialPaymentRepository.GetByCashSessionAsync(session.Id, cancellationToken);

                // Supondo que CashMath seja uma classe estática do seu domínio/aplicação
                var expected = CashMath.ExpectedCash(session.OpeningAmount, sales, movements, partials);

                // Esperado por forma de pagamento: agregado das vendas liquidadas da sessao,
                // mesma logica do GetCashSummaryQueryHandler — nunca confiamos num "esperado"
                // vindo do cliente, so no valor conferido (CountedAmount).
                var expectedByMethod = sales
                    .Where(s => s.IsActive)
                    .SelectMany(s => s.Payments)
                    .Where(p => p.IsActive)
                    .GroupBy(p => p.PaymentMethodId)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount - (p.ChangeAmount ?? 0)));

                var paymentReconciliations = new List<CashSessionPaymentReconciliation>();
                foreach (var count in request.PaymentMethodCounts ?? [])
                {
                    var methodExpected = expectedByMethod.GetValueOrDefault(count.PaymentMethodId, 0m);
                    var reconciliationResult = CashSessionPaymentReconciliation.Create(
                        session.Id, count.PaymentMethodId, methodExpected, count.CountedAmount);
                    if (reconciliationResult.IsFailure)
                        return Result.Failure<CloseCashSessionResponse>(reconciliationResult.Error);

                    paymentReconciliations.Add(reconciliationResult.Value);
                }

                var result = session.Close(request.ClosedByEmployeeId, request.ClosingAmount, expected, paymentReconciliations);
                if (result.IsFailure)
                    return Result.Failure<CloseCashSessionResponse>(result.Error);

                await _paymentReconciliationRepository.AddRangeAsync(paymentReconciliations, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(new CloseCashSessionResponse(
                    session.Id,
                    expected,
                    request.ClosingAmount,
                    session.DifferenceAmount ?? 0,
                    session.TotalDifferenceAmount ?? 0,
                    paymentReconciliations
                        .Select(r => new PaymentMethodReconciliationResponse(r.PaymentMethodId, r.ExpectedAmount, r.CountedAmount, r.DifferenceAmount))
                        .ToList()));
            });
}