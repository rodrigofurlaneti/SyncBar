using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Cash;
using SyncBar.Application.Features.Cash.GetSummary;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Cash.GetSummary;

public sealed class GetCashSummaryQueryHandlerTests
{
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ICashMovementRepository _cashMovementRepository = Substitute.For<ICashMovementRepository>();
    private readonly IOrderPartialPaymentRepository _partialPaymentRepository = Substitute.For<IOrderPartialPaymentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCashSummaryQueryHandler _handler;

    public GetCashSummaryQueryHandlerTests()
    {
        _handler = new GetCashSummaryQueryHandler(
            _cashSessionRepository, _saleRepository, _cashMovementRepository, _partialPaymentRepository,
            _logRepository, _unitOfWork);
    }

    private static Sale CreateSale(
        long cashSessionId,
        decimal subtotalAmount,
        decimal discountAmount = 0m,
        decimal serviceFeeAmount = 0m,
        long saleNumber = 1001,
        long customerOrderId = 100)
        => Sale.Create(
            branchId: 1,
            customerOrderId: customerOrderId,
            cashSessionId: cashSessionId,
            employeeId: 5,
            saleNumber: saleNumber,
            subtotalAmount: subtotalAmount,
            discountAmount: discountAmount,
            serviceFeeAmount: serviceFeeAmount).Value;

    private static CashMovement CreateMovement(long cashSessionId, long cashMovementTypeId, decimal amount)
        => CashMovement.Create(cashSessionId, cashMovementTypeId, saleId: null, employeeId: 5, amount: amount, description: null).Value;

    private static OrderPartialPayment CreatePartialPayment(long cashSessionId, long paymentMethodId, decimal amount)
        => OrderPartialPayment.Create(
            customerOrderId: 100,
            cashSessionId: cashSessionId,
            paymentMethodId: paymentMethodId,
            employeeId: 5,
            amount: amount,
            authorizationCode: null,
            payerName: null).Value;

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnNotFoundFailure()
    {
        var query = new GetCashSummaryQuery(CashSessionId: 42);
        _cashSessionRepository.GetByIdAsync(query.CashSessionId, Arg.Any<CancellationToken>())
            .Returns((CashSession?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionInactive_ShouldReturnNotFoundFailure()
    {
        var query = new GetCashSummaryQuery(CashSessionId: 42);
        var session = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 3, openingAmount: 100m).Value;
        session.Deactivate();

        _cashSessionRepository.GetByIdAsync(query.CashSessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionWithNoSalesMovementsOrPartials_ShouldReturnZeroedSummary()
    {
        var query = new GetCashSummaryQuery(CashSessionId: 42);
        var session = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 3, openingAmount: 100m).Value;

        _cashSessionRepository.GetByIdAsync(query.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<Sale>());
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<CashMovement>());
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.CashSessionId.Should().Be(session.Id);
        response.OpeningAmount.Should().Be(session.OpeningAmount);
        response.SalesCount.Should().Be(0);
        response.SalesTotal.Should().Be(0m);
        response.PaymentTotals.Should().BeEmpty();
        response.SuprimentoTotal.Should().Be(0m);
        response.SangriaTotal.Should().Be(0m);
        response.DespesaTotal.Should().Be(0m);
        response.PartialPaymentsTotal.Should().Be(0m);

        // Não reimplementamos a fórmula: chamamos a própria CashMath.ExpectedCash com os
        // mesmos argumentos para obter o valor esperado.
        var expectedCash = CashMath.ExpectedCash(session.OpeningAmount, Array.Empty<Sale>(), Array.Empty<CashMovement>(), Array.Empty<OrderPartialPayment>());
        response.ExpectedCashAmount.Should().Be(expectedCash);
        response.ExpectedCashAmount.Should().Be(session.OpeningAmount);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSalesMovementsAndPartials_ShouldAggregateCorrectlyAndExcludeInactiveEntries()
    {
        var query = new GetCashSummaryQuery(CashSessionId: 42);
        var session = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 3, openingAmount: 100m).Value;

        var activeSale = CreateSale(session.Id, subtotalAmount: 100m, saleNumber: 1001);
        activeSale.AddPayment(paymentMethodId: PaymentMethodIds.Dinheiro, amount: 50m, changeAmount: 5m, authorizationCode: null, allowsChange: true)
            .IsSuccess.Should().BeTrue();
        activeSale.AddPayment(paymentMethodId: PaymentMethodIds.CartaoCredito, amount: 30m, changeAmount: null, authorizationCode: "AUTH1", allowsChange: false)
            .IsSuccess.Should().BeTrue();
        activeSale.AddPayment(paymentMethodId: PaymentMethodIds.Pix, amount: 20m, changeAmount: null, authorizationCode: null, allowsChange: false)
            .IsSuccess.Should().BeTrue();
        // Este pagamento é desativado e não deve entrar nos totais nem em PaymentTotals.
        activeSale.Payments.Single(p => p.PaymentMethodId == PaymentMethodIds.Pix).Deactivate();

        // Venda inativa: deve ser ignorada por completo, mesmo tendo pagamento ativo.
        var inactiveSale = CreateSale(session.Id, subtotalAmount: 999m, saleNumber: 1002, customerOrderId: 200);
        inactiveSale.AddPayment(paymentMethodId: PaymentMethodIds.Dinheiro, amount: 999m, changeAmount: null, authorizationCode: null, allowsChange: true)
            .IsSuccess.Should().BeTrue();
        inactiveSale.Deactivate();

        var sales = new List<Sale> { activeSale, inactiveSale };

        var suprimento = CreateMovement(session.Id, CashMovementTypeIds.Suprimento, 40m);
        var sangria = CreateMovement(session.Id, CashMovementTypeIds.Sangria, 15m);
        var despesa = CreateMovement(session.Id, CashMovementTypeIds.Despesa, 10m);
        var movements = new List<CashMovement> { suprimento, sangria, despesa };

        var partial = CreatePartialPayment(session.Id, PaymentMethodIds.Pix, 25m);
        var partials = new List<OrderPartialPayment> { partial };

        _cashSessionRepository.GetByIdAsync(query.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _saleRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(sales);
        _cashMovementRepository.GetBySessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(movements);
        _partialPaymentRepository.GetByCashSessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(partials);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;

        response.SalesCount.Should().Be(1);
        response.SalesTotal.Should().Be(activeSale.TotalAmount);

        // Agrupado e ordenado por PaymentMethodId: Dinheiro(1) antes de CartaoCredito(2); Pix(4)
        // foi excluído por estar com o pagamento desativado.
        response.PaymentTotals.Should().HaveCount(2);
        response.PaymentTotals.ElementAt(0).PaymentMethodId.Should().Be(PaymentMethodIds.Dinheiro);
        response.PaymentTotals.ElementAt(0).TotalAmount.Should().Be(45m); // 50 - 5 de troco
        response.PaymentTotals.ElementAt(1).PaymentMethodId.Should().Be(PaymentMethodIds.CartaoCredito);
        response.PaymentTotals.ElementAt(1).TotalAmount.Should().Be(30m);

        response.SuprimentoTotal.Should().Be(40m);
        response.SangriaTotal.Should().Be(15m);
        response.DespesaTotal.Should().Be(10m);
        response.PartialPaymentsTotal.Should().Be(25m);

        var expectedCash = CashMath.ExpectedCash(session.OpeningAmount, sales, movements, partials);
        response.ExpectedCashAmount.Should().Be(expectedCash);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
