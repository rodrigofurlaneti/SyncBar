using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Billing.RegisterSale;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class RegisterSaleCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegisterSaleCommandHandler CreateHandler()
        => new(_orderRepository, _saleRepository, _cashSessionRepository, _diningTableRepository,
            _comandaRepository, _productRepository, _stockItemRepository, _stockMovementRepository, _unitOfWork);

    private static CustomerOrder ClosedOrder(decimal price = 100m)
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(5, price, 1, null, null);
        order.Close(0.10m);
        return order;
    }

    private static CashSession OpenSession() => CashSession.Open(1, 1, 100m).Value;

    [Fact]
    public async Task Handle_WithSplitPayments_ShouldRegisterSaleAndMarkOrderPaid()
    {
        // Mesa com 3 pessoas: 2 no crédito, 1 em dinheiro (com troco).
        var order = ClosedOrder(100m); // total 110 com taxa
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(OpenSession());
        _saleRepository.ExistsActiveByOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);
        _saleRepository.GetNextSaleNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1L);

        var payments = new List<SalePaymentInput>
        {
            new(PaymentMethodIds.CartaoCredito, 40m, null, "AUT-001"),
            new(PaymentMethodIds.CartaoCredito, 40m, null, "AUT-002"),
            new(PaymentMethodIds.Dinheiro, 50m, 20m, null), // pagou 50, troco 20 → líquido 30
        };

        var result = await CreateHandler().Handle(
            new RegisterSaleCommand(1, 7, 1, payments), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatusId.Should().Be(OrderStatusIds.Pago);
        await _saleRepository.Received(1).AddAsync(
            Arg.Is<Sale>(s => s.Payments.Count == 3 && s.TotalAmount == 110m), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInsufficientPayment_ShouldFail()
    {
        var order = ClosedOrder(100m); // total 110
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(OpenSession());
        _saleRepository.ExistsActiveByOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);
        _saleRepository.GetNextSaleNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1L);

        var payments = new List<SalePaymentInput> { new(PaymentMethodIds.Pix, 50m, null, "E2E-123") };

        var result = await CreateHandler().Handle(
            new RegisterSaleCommand(1, 7, 1, payments), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.InsufficientPayment");
        await _saleRepository.DidNotReceive().AddAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithChangeOnCardPayment_ShouldFail()
    {
        var order = ClosedOrder(100m);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(OpenSession());
        _saleRepository.ExistsActiveByOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);
        _saleRepository.GetNextSaleNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1L);

        var payments = new List<SalePaymentInput> { new(PaymentMethodIds.CartaoDebito, 120m, 10m, "AUT-9") };

        var result = await CreateHandler().Handle(
            new RegisterSaleCommand(1, 7, 1, payments), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.ChangeNotAllowed");
    }

    [Fact]
    public async Task Handle_WithOrderNotClosed_ShouldFail()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value; // Aberto
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var result = await CreateHandler().Handle(
            new RegisterSaleCommand(1, 7, 1, [new(PaymentMethodIds.Pix, 10m, null, null)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.OrderNotAwaitingPayment");
    }

    [Fact]
    public async Task Handle_WithClosedCashSession_ShouldFail()
    {
        var order = ClosedOrder();
        var session = OpenSession();
        session.Close(1, 100m, 100m);
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateHandler().Handle(
            new RegisterSaleCommand(1, 7, 1, [new(PaymentMethodIds.Pix, 110m, null, null)]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotOpen");
    }
}
