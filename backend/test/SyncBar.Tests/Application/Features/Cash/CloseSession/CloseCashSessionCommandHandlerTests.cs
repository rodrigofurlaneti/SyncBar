using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Cash;
using SyncBar.Application.Features.Cash.CloseSession;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Cash.CloseSession;

public sealed class CloseCashSessionCommandHandlerTests
{
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ICashMovementRepository _cashMovementRepository = Substitute.For<ICashMovementRepository>();
    private readonly IOrderPartialPaymentRepository _partialPaymentRepository = Substitute.For<IOrderPartialPaymentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CloseCashSessionCommandHandler _handler;

    public CloseCashSessionCommandHandlerTests()
    {
        _handler = new CloseCashSessionCommandHandler(
            _cashSessionRepository, _saleRepository, _cashMovementRepository, _partialPaymentRepository,
            _logRepository, _unitOfWork);
    }

    private static CashSession CreateOpenSession(decimal openingAmount = 100m)
        => CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount).Value;

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnFailureWithoutQueryingMovementsOrClosing()
    {
        var command = new CloseCashSessionCommand(CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: 100m);
        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns((CashSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotFound");

        await _saleRepository.DidNotReceive().GetByCashSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _cashMovementRepository.DidNotReceive().GetBySessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _partialPaymentRepository.DidNotReceive().GetByCashSessionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionInactive_ShouldReturnFailure()
    {
        var session = CreateOpenSession();
        session.Deactivate();
        var command = new CloseCashSessionCommand(CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: 100m);
        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoSalesMovementsOrPartials_ShouldCloseWithExpectedEqualToOpeningAmount()
    {
        var session = CreateOpenSession(openingAmount: 150m);
        var command = new CloseCashSessionCommand(CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: 150m);
        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Sale>());
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CashMovement>());
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrderPartialPayment>());

        var expected = CashMath.ExpectedCash(session.OpeningAmount, Array.Empty<Sale>(), Array.Empty<CashMovement>(), Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CashSessionId.Should().Be(session.Id);
        result.Value.ExpectedAmount.Should().Be(expected);
        result.Value.ExpectedAmount.Should().Be(session.OpeningAmount);
        result.Value.ClosingAmount.Should().Be(command.ClosingAmount);
        result.Value.DifferenceAmount.Should().Be(session.DifferenceAmount ?? 0);

        session.CashSessionStatusId.Should().Be(CashSessionStatusIds.Fechado);
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSaleMovementsAndPartialPayment_ShouldComputeExpectedFromCashMath()
    {
        var session = CreateOpenSession(openingAmount: 100m);
        var command = new CloseCashSessionCommand(CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: 300m);

        var sale = Sale.Create(
            branchId: 1, customerOrderId: 1, cashSessionId: session.Id, employeeId: 10,
            saleNumber: 1, subtotalAmount: 80m, discountAmount: 0m, serviceFeeAmount: 0m).Value;
        sale.AddPayment(PaymentMethodIds.Dinheiro, amount: 80m, changeAmount: null, authorizationCode: null, allowsChange: false);

        var suprimento = CashMovement.Create(session.Id, CashMovementTypeIds.Suprimento, null, 10, 50m, "Reforço de troco").Value;
        var sangria = CashMovement.Create(session.Id, CashMovementTypeIds.Sangria, null, 10, 20m, "Sangria").Value;
        var movements = new[] { suprimento, sangria };

        var partial = OrderPartialPayment.Create(
            customerOrderId: 2, cashSessionId: session.Id, paymentMethodId: PaymentMethodIds.Dinheiro,
            employeeId: 10, amount: 30m, authorizationCode: null, payerName: null).Value;

        var sales = new[] { sale };
        var partials = new[] { partial };

        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(sales);
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(movements);
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(partials);

        var expected = CashMath.ExpectedCash(session.OpeningAmount, sales, movements, partials);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpectedAmount.Should().Be(expected);
        // 100 (abertura) + 50 (suprimento) - 20 (sangria) + 80 (venda dinheiro) + 30 (parcial dinheiro) = 240.
        expected.Should().Be(240m);
        result.Value.DifferenceAmount.Should().Be(command.ClosingAmount - expected);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionAlreadyClosed_ShouldPropagateDomainFailureWithoutExplicitCommit()
    {
        var session = CreateOpenSession(openingAmount: 100m);
        // Fecha a sessão diretamente via API pública, simulando uma sessão já fechada
        // quando o handler tentar fechá-la de novo (Close falha por status != Aberto).
        session.Close(closedByEmployeeId: 10, closingAmount: 100m, expectedAmount: 100m);

        var command = new CloseCashSessionCommand(CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: 100m);
        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Sale>());
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CashMovement>());
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotOpen");
        // Sem commit explícito nesse ramo: só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeClosingAmount_ShouldPropagateDomainFailureWithoutExplicitCommit()
    {
        var session = CreateOpenSession(openingAmount: 100m);
        var command = new CloseCashSessionCommand(CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: -1m);
        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Sale>());
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CashMovement>());
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.InvalidClosingAmount");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
