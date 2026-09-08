using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Cash;
using SyncBar.Application.Features.Cash.CloseSession;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Cash.CloseSession;

public sealed class CloseCashSessionCommandHandlerTests
{
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ICashMovementRepository _cashMovementRepository = Substitute.For<ICashMovementRepository>();
    private readonly IOrderPartialPaymentRepository _partialPaymentRepository = Substitute.For<IOrderPartialPaymentRepository>();
    private readonly ICashSessionPaymentReconciliationRepository _paymentReconciliationRepository = Substitute.For<ICashSessionPaymentReconciliationRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CloseCashSessionCommandHandler _handler;

    public CloseCashSessionCommandHandlerTests()
    {
        _handler = new CloseCashSessionCommandHandler(
            _cashSessionRepository, _saleRepository, _cashMovementRepository, _partialPaymentRepository,
            _paymentReconciliationRepository, _logRepository, _unitOfWork);
    }

    private static CashSession CreateOpenSession(decimal openingAmount = 100m)
        => CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount).Value;

    // CashSessionPaymentReconciliation.Create valida CashSessionId > 0 (e uma FK real). Como a
    // fabrica publica do Entity/AggregateRoot nao expoe forma de fixar o Id (so existiria apos o
    // SaveChanges do EF Core), setamos via reflection para simular o Id que o EF ja teria
    // atribuido a session antes do close — mesmo padrao usado em CloseShiftClosingCommandHandlerTests.
    private static void SetId(Entity entity, long id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

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

    [Fact]
    public async Task Handle_WithPaymentMethodCounts_ShouldPersistReconciliationsAndComputeTotalDifference()
    {
        var session = CreateOpenSession(openingAmount: 100m);
        SetId(session, 1);
        var command = new CloseCashSessionCommand(
            CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: 100m,
            PaymentMethodCounts: [new PaymentMethodCountRequest(PaymentMethodIds.CartaoCredito, CountedAmount: 190m)]);

        var creditSale = Sale.Create(
            branchId: 1, customerOrderId: 1, cashSessionId: session.Id, employeeId: 10,
            saleNumber: 1, subtotalAmount: 200m, discountAmount: 0m, serviceFeeAmount: 0m).Value;
        creditSale.AddPayment(PaymentMethodIds.CartaoCredito, amount: 200m, changeAmount: null, authorizationCode: "AUTH1", allowsChange: false);

        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns([creditSale]);
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<CashMovement>());
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reconciliation = result.Value.PaymentReconciliations.Should().ContainSingle().Subject;
        reconciliation.PaymentMethodId.Should().Be(PaymentMethodIds.CartaoCredito);
        reconciliation.ExpectedAmount.Should().Be(200m);
        reconciliation.CountedAmount.Should().Be(190m);
        reconciliation.DifferenceAmount.Should().Be(-10m);

        // Dinheiro esperado = 100 (abertura), conferido = 100 => diferenca 0. Total geral = 0 + (-10) do credito.
        result.Value.DifferenceAmount.Should().Be(0m);
        result.Value.TotalDifferenceAmount.Should().Be(-10m);
        session.TotalDifferenceAmount.Should().Be(-10m);

        await _paymentReconciliationRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<CashSessionPaymentReconciliation>>(list => list.Count() == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PaymentMethodCountForMethodWithNoSales_ShouldUseZeroAsExpected()
    {
        var session = CreateOpenSession(openingAmount: 100m);
        SetId(session, 1);
        var command = new CloseCashSessionCommand(
            CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: 100m,
            PaymentMethodCounts: [new PaymentMethodCountRequest(PaymentMethodIds.Pix, CountedAmount: 50m)]);

        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<Sale>());
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<CashMovement>());
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reconciliation = result.Value.PaymentReconciliations.Should().ContainSingle().Subject;
        reconciliation.ExpectedAmount.Should().Be(0m);
        reconciliation.DifferenceAmount.Should().Be(50m);
        result.Value.TotalDifferenceAmount.Should().Be(50m);
    }

    [Fact]
    public async Task Handle_InvalidPaymentMethodInCounts_ShouldReturnFailureAndNotCloseSession()
    {
        var session = CreateOpenSession(openingAmount: 100m);
        SetId(session, 1);
        var command = new CloseCashSessionCommand(
            CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: 100m,
            PaymentMethodCounts: [new PaymentMethodCountRequest(PaymentMethodId: 0, CountedAmount: 10m)]);

        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<Sale>());
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<CashMovement>());
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSessionPaymentReconciliation.InvalidPaymentMethod");
        session.IsOpen().Should().BeTrue();
        await _paymentReconciliationRepository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<CashSessionPaymentReconciliation>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeCountedAmountInCounts_ShouldReturnFailureAndNotCloseSession()
    {
        var session = CreateOpenSession(openingAmount: 100m);
        SetId(session, 1);
        var command = new CloseCashSessionCommand(
            CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: 100m,
            PaymentMethodCounts: [new PaymentMethodCountRequest(PaymentMethodIds.Pix, CountedAmount: -5m)]);

        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<Sale>());
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<CashMovement>());
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSessionPaymentReconciliation.InvalidCountedAmount");
        session.IsOpen().Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoPaymentMethodCounts_ShouldLeaveTotalDifferenceEqualToCashDifference()
    {
        var session = CreateOpenSession(openingAmount: 100m);
        var command = new CloseCashSessionCommand(CashSessionId: 1, ClosedByEmployeeId: 10, ClosingAmount: 90m);
        _cashSessionRepository.GetByIdForUpdateAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<Sale>());
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<CashMovement>());
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentReconciliations.Should().BeEmpty();
        result.Value.TotalDifferenceAmount.Should().Be(result.Value.DifferenceAmount);
        await _paymentReconciliationRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<CashSessionPaymentReconciliation>>(list => !list.Any()), Arg.Any<CancellationToken>());
    }
}
