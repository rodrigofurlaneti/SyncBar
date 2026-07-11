using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Billing.RegisterPartialPayment;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class RegisterPartialPaymentCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly IOrderPartialPaymentRepository _partialRepository = Substitute.For<IOrderPartialPaymentRepository>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegisterPartialPaymentCommandHandler CreateHandler()
        => new(_orderRepository, _cashSessionRepository, _partialRepository, _printingService, _unitOfWork);

    private static CustomerOrder TableOrder(decimal itemPrice = 245m)
    {
        var order = CustomerOrder.Create(1, 10, null, 1, 4, null).Value;
        order.AddItem(1, itemPrice, 1, null, null);
        return order;
    }

    [Fact]
    public async Task Handle_TableOrderWithOpenSession_ShouldRegisterAndPrintReceipt()
    {
        // Conta de 245; cliente que saiu deixou 65,55 — mesa continua aberta.
        var order = TableOrder(245m);
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(CashSession.Open(1, 1, 100m).Value);
        _partialRepository.GetByOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<OrderPartialPayment>());

        var result = await CreateHandler().Handle(new RegisterPartialPaymentCommand(
            1, 7, 2, PaymentMethodIds.Pix, 65.55m, "E2E-99", "Carlos"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatusId.Should().NotBe(OrderStatusIds.Pago); // mesa segue aberta
        await _partialRepository.Received(1).AddAsync(
            Arg.Is<OrderPartialPayment>(p => p.Amount == 65.55m && p.PayerName == "Carlos"),
            Arg.Any<CancellationToken>());
        await _printingService.Received(1).PrintPartialReceiptAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComandaOrder_ShouldFail()
    {
        var comandaOrder = CustomerOrder.Create(1, null, 37, 1, null, null).Value;
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(comandaOrder);

        var result = await CreateHandler().Handle(new RegisterPartialPaymentCommand(
            1, 7, 2, PaymentMethodIds.Dinheiro, 20m, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PartialPayment.TableOnly");
    }

    [Fact]
    public async Task Handle_AmountAboveRemaining_ShouldFail()
    {
        var order = TableOrder(100m);
        _orderRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _cashSessionRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(CashSession.Open(1, 1, 100m).Value);
        _partialRepository.GetByOrderAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<OrderPartialPayment>
            {
                OrderPartialPayment.Create(1, 7, PaymentMethodIds.Dinheiro, 1, 60m, null, null).Value,
            });

        // Restante = 40; tentar pagar 50 deve falhar.
        var result = await CreateHandler().Handle(new RegisterPartialPaymentCommand(
            1, 7, 2, PaymentMethodIds.Dinheiro, 50m, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PartialPayment.ExceedsRemaining");
    }
}
