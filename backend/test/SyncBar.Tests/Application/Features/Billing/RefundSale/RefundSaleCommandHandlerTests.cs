using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Billing.RefundSale;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Billing.RefundSale;

public sealed class RefundSaleCommandHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ICashMovementRepository _cashMovementRepository = Substitute.For<ICashMovementRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    private readonly RefundSaleCommandHandler _handler;

    public RefundSaleCommandHandlerTests()
    {
        // TimeProvider.GetLocalNow() NÃO é virtual — não dá pra interceptar a chamada em si.
        // A implementação real dela chama GetUtcNow() e LocalTimeZone (esses sim virtuais),
        // então é isso que precisa ser configurado no substituto.
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero));
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);

        _handler = new RefundSaleCommandHandler(
            _saleRepository, _orderRepository, _cashSessionRepository, _cashMovementRepository,
            _diningTableRepository, _comandaRepository, _unitOfWork, _timeProvider);
    }

    private static Sale CreateActiveSale(long customerOrderId, long cashSessionId, long saleNumber = 1, decimal subtotal = 100m)
        => Sale.Create(
            branchId: 1, customerOrderId: customerOrderId, cashSessionId: cashSessionId, employeeId: 1,
            saleNumber: saleNumber, subtotalAmount: subtotal, discountAmount: 0m, serviceFeeAmount: 0m).Value;

    private static CashSession CreateOpenSession()
        => CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 1, openingAmount: 100m).Value;

    private static CashSession CreateClosedSession()
    {
        var session = CreateOpenSession();
        session.Close(closedByEmployeeId: 1, closingAmount: 100m, expectedAmount: 100m);
        return session;
    }

    private static CustomerOrder CreateOpenOrder(long? diningTableId, long? comandaId)
        => CustomerOrder.Create(
            branchId: 1, diningTableId: diningTableId, comandaId: comandaId, employeeId: 1,
            guestCount: null, notes: null, Now: DateTime.Now).Value;

    // Reproduz o fluxo real de negocio ate o status Pago: abrir -> lancar item -> fechar -> pagar.
    private static CustomerOrder CreatePaidOrder(long? diningTableId, long? comandaId)
    {
        var order = CreateOpenOrder(diningTableId, comandaId);
        order.AddItem(productId: 1, unitPrice: 50m, quantity: 1m, notes: null, employeeId: 1, Now: DateTime.Now);
        order.Close(serviceFeeRate: 0m, Now: DateTime.Now);
        order.MarkAsPaid(Now: DateTime.Now);
        return order;
    }

    private static DiningTable CreateTable()
        => DiningTable.Create(branchId: 1, tableStatusId: TableStatusIds.Ocupada, number: 5, capacity: 4).Value;

    private static Comanda CreateComanda()
        => Comanda.Create(branchId: 1, comandaStatusId: ComandaStatusIds.EmUso, code: "C01").Value;

    [Fact]
    public async Task Handle_SaleNotFound_ShouldReturnFailureWithoutCommitting()
    {
        var command = new RefundSaleCommand(SaleId: 1, EmployeeId: 5, Reason: null);
        _saleRepository.GetByIdForUpdateAsync(command.SaleId, Arg.Any<CancellationToken>()).Returns((Sale?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.NotFound");
        await _cashSessionRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SaleInactive_ShouldReturnFailureWithoutCommitting()
    {
        var sale = CreateActiveSale(customerOrderId: 1, cashSessionId: 1);
        sale.Deactivate();
        var command = new RefundSaleCommand(SaleId: 1, EmployeeId: 5, Reason: null);
        _saleRepository.GetByIdForUpdateAsync(command.SaleId, Arg.Any<CancellationToken>()).Returns(sale);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.NotFound");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CashSessionNotFound_ShouldReturnFailureAndKeepSaleActive()
    {
        var sale = CreateActiveSale(customerOrderId: 1, cashSessionId: 1);
        var command = new RefundSaleCommand(SaleId: 1, EmployeeId: 5, Reason: null);
        _saleRepository.GetByIdForUpdateAsync(command.SaleId, Arg.Any<CancellationToken>()).Returns(sale);
        _cashSessionRepository.GetByIdAsync(sale.CashSessionId, Arg.Any<CancellationToken>()).Returns((CashSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.SessionClosed");
        // A checagem de sessao vem antes de sale.Deactivate() no handler.
        sale.IsActive.Should().BeTrue();
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CashSessionAlreadyClosed_ShouldReturnFailureAndKeepSaleActive()
    {
        var sale = CreateActiveSale(customerOrderId: 1, cashSessionId: 1);
        var session = CreateClosedSession();
        var command = new RefundSaleCommand(SaleId: 1, EmployeeId: 5, Reason: null);
        _saleRepository.GetByIdForUpdateAsync(command.SaleId, Arg.Any<CancellationToken>()).Returns(sale);
        _cashSessionRepository.GetByIdAsync(sale.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.SessionClosed");
        sale.IsActive.Should().BeTrue();
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderNotPaid_ShouldReturnReopenFailureAfterDeactivatingSaleInMemoryWithoutCommitting()
    {
        var sale = CreateActiveSale(customerOrderId: 42, cashSessionId: 1);
        var session = CreateOpenSession();
        // Pedido ainda aberto (status Aberto): ReopenForPayment so aceita pedidos com status Pago.
        var order = CreateOpenOrder(diningTableId: 10, comandaId: null);
        var command = new RefundSaleCommand(SaleId: 1, EmployeeId: 5, Reason: null);
        _saleRepository.GetByIdForUpdateAsync(command.SaleId, Arg.Any<CancellationToken>()).Returns(sale);
        _cashSessionRepository.GetByIdAsync(sale.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _orderRepository.GetByIdForUpdateAsync(sale.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotPaid");
        // sale.Deactivate() ja rodou (acontece antes da tentativa de reabertura do pedido),
        // mas como o handler retorna antes do CommitAsync, essa mudanca nunca e persistida.
        sale.IsActive.Should().BeFalse();
        await _diningTableRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _comandaRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _cashMovementRepository.DidNotReceive().AddAsync(Arg.Any<CashMovement>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRefundWithTableAndComanda_ShouldReopenOrderReleaseTableAndComandaAndRegisterMovement()
    {
        var sale = CreateActiveSale(customerOrderId: 42, cashSessionId: 1, saleNumber: 777, subtotal: 120m);
        var session = CreateOpenSession();
        var order = CreatePaidOrder(diningTableId: 10, comandaId: 20);
        var table = CreateTable();
        var comanda = CreateComanda();
        var command = new RefundSaleCommand(SaleId: 1, EmployeeId: 5, Reason: "  Cliente desistiu  ");

        _saleRepository.GetByIdForUpdateAsync(command.SaleId, Arg.Any<CancellationToken>()).Returns(sale);
        _cashSessionRepository.GetByIdAsync(sale.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _orderRepository.GetByIdForUpdateAsync(sale.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId!.Value, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByIdForUpdateAsync(order.ComandaId!.Value, Arg.Any<CancellationToken>()).Returns(comanda);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sale.IsActive.Should().BeFalse();
        order.OrderStatusId.Should().Be(OrderStatusIds.AguardandoPagamento);
        table.TableStatusId.Should().Be(TableStatusIds.EmFechamento);
        comanda.ComandaStatusId.Should().Be(ComandaStatusIds.EmUso);

        // CashMovement.Create atualmente nao tem invariantes (sempre retorna sucesso), entao
        // o AddAsync sempre deve ocorrer neste fluxo feliz.
        await _cashMovementRepository.Received(1).AddAsync(
            Arg.Is<CashMovement>(m =>
                m.CashSessionId == sale.CashSessionId &&
                m.CashMovementTypeId == CashMovementTypeIds.EstornoVenda &&
                m.EmployeeId == command.EmployeeId &&
                m.Amount == sale.TotalAmount &&
                m.Description == "Cliente desistiu"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SaleWithoutAssociatedOrder_ShouldStillRegisterMovementWithDefaultReasonAndCommit()
    {
        var sale = CreateActiveSale(customerOrderId: 999, cashSessionId: 1, saleNumber: 321, subtotal: 55m);
        var session = CreateOpenSession();
        // Reason em branco -> handler usa a mensagem padrao com o numero da venda.
        var command = new RefundSaleCommand(SaleId: 1, EmployeeId: 5, Reason: "   ");

        _saleRepository.GetByIdForUpdateAsync(command.SaleId, Arg.Any<CancellationToken>()).Returns(sale);
        _cashSessionRepository.GetByIdAsync(sale.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _orderRepository.GetByIdForUpdateAsync(sale.CustomerOrderId, Arg.Any<CancellationToken>()).Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _diningTableRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _comandaRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());

        await _cashMovementRepository.Received(1).AddAsync(
            Arg.Is<CashMovement>(m => m.Description == $"Estorno da venda #{sale.SaleNumber}"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
