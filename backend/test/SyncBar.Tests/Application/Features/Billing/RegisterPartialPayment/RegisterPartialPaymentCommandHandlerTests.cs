using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Billing.RegisterPartialPayment;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Billing.RegisterPartialPayment;

public sealed class RegisterPartialPaymentCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly IOrderPartialPaymentRepository _partialPaymentRepository = Substitute.For<IOrderPartialPaymentRepository>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RegisterPartialPaymentCommandHandler _handler;

    public RegisterPartialPaymentCommandHandlerTests()
    {
        _handler = new RegisterPartialPaymentCommandHandler(
            _orderRepository, _cashSessionRepository, _partialPaymentRepository, _printingService,
            _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateOrder(long? diningTableId, long? comandaId = null)
        => CustomerOrder.Create(
            branchId: 1, diningTableId: diningTableId, comandaId: comandaId, employeeId: 1,
            guestCount: null, notes: null, Now: DateTime.Now).Value;

    // Pedido de mesa com um item lancado (TotalAmount = amount), ainda aberto/em andamento.
    private static CustomerOrder CreateOrderWithBalance(decimal amount, long diningTableId = 10)
    {
        var order = CreateOrder(diningTableId);
        order.AddItem(productId: 1, unitPrice: amount, quantity: 1m, notes: null, employeeId: 1, Now: DateTime.Now);
        return order;
    }

    private static CashSession CreateOpenSession()
        => CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 1, openingAmount: 100m).Value;

    private static CashSession CreateClosedSession()
    {
        var session = CreateOpenSession();
        session.Close(closedByEmployeeId: 1, closingAmount: 100m, expectedAmount: 100m);
        return session;
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new RegisterPartialPaymentCommand(
            CustomerOrderId: 1, CashSessionId: 1, EmployeeId: 5, PaymentMethodId: 1,
            Amount: 10m, AuthorizationCode: null, PayerName: null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _cashSessionRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        // Sem commit explicito nesse ramo: so o commit do finally do BaseCommandHandler.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderInactive_ShouldReturnFailure()
    {
        var order = CreateOrder(diningTableId: 10);
        order.Deactivate(DateTime.Now);
        var command = new RegisterPartialPaymentCommand(order.Id, 1, 5, 1, 10m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderWithoutDiningTable_ShouldReturnTableOnlyFailure()
    {
        // Pedido de comanda (sem mesa) - pagamento parcial so e permitido em contas de mesa.
        var order = CreateOrder(diningTableId: null, comandaId: 7);
        var command = new RegisterPartialPaymentCommand(order.Id, 1, 5, 1, 10m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PartialPayment.TableOnly");
        await _cashSessionRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderAlreadyCancelled_ShouldReturnOrderClosedFailure()
    {
        var order = CreateOrder(diningTableId: 10);
        order.Cancel(DateTime.Now);
        var command = new RegisterPartialPaymentCommand(order.Id, 1, 5, 1, 10m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PartialPayment.OrderClosed");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderAlreadyPaid_ShouldReturnOrderClosedFailure()
    {
        var order = CreateOrder(diningTableId: 10);
        order.AddItem(productId: 1, unitPrice: 50m, quantity: 1m, notes: null, employeeId: 1, Now: DateTime.Now);
        order.Close(serviceFeeRate: 0m, Now: DateTime.Now);
        order.MarkAsPaid(Now: DateTime.Now);
        var command = new RegisterPartialPaymentCommand(order.Id, 1, 5, 1, 10m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PartialPayment.OrderClosed");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CashSessionNotFound_ShouldReturnFailure()
    {
        var order = CreateOrderWithBalance(100m);
        var command = new RegisterPartialPaymentCommand(order.Id, 1, 5, 1, 10m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns((CashSession?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotOpen");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CashSessionClosed_ShouldReturnFailure()
    {
        var order = CreateOrderWithBalance(100m);
        var session = CreateClosedSession();
        var command = new RegisterPartialPaymentCommand(order.Id, 1, 5, 1, 10m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotOpen");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CashSessionInactiveButStillOpenStatus_ShouldReturnFailure()
    {
        var order = CreateOrderWithBalance(100m);
        var session = CreateOpenSession();
        session.Deactivate();
        var command = new RegisterPartialPaymentCommand(order.Id, 1, 5, 1, 10m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CashSession.NotOpen");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderWithNoRemainingBalance_ShouldReturnNothingRemainingFailure()
    {
        var order = CreateOrder(diningTableId: 10); // sem itens -> TotalAmount = 0
        var session = CreateOpenSession();
        var command = new RegisterPartialPaymentCommand(order.Id, 1, 5, 1, 10m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _partialPaymentRepository.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PartialPayment.NothingRemaining");
        await _partialPaymentRepository.DidNotReceive().AddAsync(Arg.Any<OrderPartialPayment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AmountExceedsRemaining_ShouldReturnExceedsRemainingFailure()
    {
        var order = CreateOrderWithBalance(100m);
        var session = CreateOpenSession();
        var command = new RegisterPartialPaymentCommand(order.Id, session.Id, 5, 1, 150m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _partialPaymentRepository.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PartialPayment.ExceedsRemaining");
        // A mensagem usa a mesma formatacao (0.00) dependente de cultura do handler - reproduzimos aqui.
        result.Error.Message.Should().Be(
            $"Valor ({command.Amount:0.00}) excede o restante da conta ({order.TotalAmount:0.00}).");
        await _partialPaymentRepository.DidNotReceive().AddAsync(Arg.Any<OrderPartialPayment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidPartialPayment_ShouldPersistCommitTwiceAndTriggerPrinting()
    {
        var order = CreateOrderWithBalance(100m);
        var session = CreateOpenSession();
        var command = new RegisterPartialPaymentCommand(
            order.Id, session.Id, EmployeeId: 5, PaymentMethodId: 2,
            Amount: 40m, AuthorizationCode: "AUTH1", PayerName: "Fulano");
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _partialPaymentRepository.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrderPartialPayment>());
        _printingService.PrintPartialReceiptAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _partialPaymentRepository.Received(1).AddAsync(
            Arg.Is<OrderPartialPayment>(p =>
                p.CustomerOrderId == order.Id &&
                p.CashSessionId == session.Id &&
                p.PaymentMethodId == command.PaymentMethodId &&
                p.EmployeeId == command.EmployeeId &&
                p.Amount == command.Amount &&
                p.AuthorizationCode == command.AuthorizationCode &&
                p.PayerName == command.PayerName),
            Arg.Any<CancellationToken>());
        // Commit explicito do handler (apos AddAsync) + commit do finally do BaseCommandHandler.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
        await _printingService.Received(1).PrintPartialReceiptAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PrintingThrows_ShouldStillReturnSuccessBecauseFailureIsSwallowed()
    {
        var order = CreateOrderWithBalance(100m);
        var session = CreateOpenSession();
        var command = new RegisterPartialPaymentCommand(order.Id, session.Id, 5, 1, 40m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _partialPaymentRepository.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrderPartialPayment>());
        _printingService.PrintPartialReceiptAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Impressora offline"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _partialPaymentRepository.Received(1).AddAsync(Arg.Any<OrderPartialPayment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RequestedAmountIsZeroOrLess_ShouldReturnInvalidAmountFailureWithoutPersistingOrPrinting()
    {
        var order = CreateOrderWithBalance(100m);
        var session = CreateOpenSession();
        // 0 nao excede o restante (falha a checagem de ExceedsRemaining), mas OrderPartialPayment.Create
        // exige amount > 0 - falha mais adiante, dentro da fabrica da entidade.
        var command = new RegisterPartialPaymentCommand(order.Id, session.Id, 5, 1, 0m, null, null);
        _orderRepository.GetByIdAsync(command.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(command.CashSessionId, Arg.Any<CancellationToken>()).Returns(session);
        _partialPaymentRepository.GetByOrderAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OrderPartialPayment>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PartialPayment.InvalidAmount");
        await _partialPaymentRepository.DidNotReceive().AddAsync(Arg.Any<OrderPartialPayment>(), Arg.Any<CancellationToken>());
        await _printingService.DidNotReceive().PrintPartialReceiptAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        // Sem commit explicito nesse ramo: so o commit do finally do BaseCommandHandler.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}