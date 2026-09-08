using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.StartPreparation;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.StartPreparation;

public sealed class StartOrderPreparationCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly StartOrderPreparationCommandHandler _handler;

    public StartOrderPreparationCommandHandlerTests()
    {
        _handler = new StartOrderPreparationCommandHandler(
            _orderRepository, TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateOpenOrder()
        => CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new StartOrderPreparationCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // Cobre o bug real: pedidos do iFood cujos itens não conciliaram por EAN chegam sem nenhum
    // item lançado — AddItem nunca roda, então o pedido fica preso em Aberto (1) e o botão
    // "Enviar p/ cozinha" (que só atualizava status de item) não tinha nada a fazer. Este comando
    // promove o pedido explicitamente, independente de haver itens pendentes.
    [Fact]
    public async Task Handle_OrderIsAberto_ShouldPromoteToEmAndamentoAndCommitTwice()
    {
        var order = CreateOpenOrder(); // Aberto, sem itens
        var command = new StartOrderPreparationCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatusId.Should().Be(OrderStatusIds.EmAndamento);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderAlreadyEmAndamento_ShouldBeNoOpAndStillSucceed()
    {
        var order = CreateOpenOrder();
        order.AddItem(productId: 1, unitPrice: 10m, quantity: 1, notes: null, employeeId: null, DateTime.Now);
        var command = new StartOrderPreparationCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.OrderStatusId.Should().Be(OrderStatusIds.EmAndamento);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderNotOpen_ShouldReturnFailure()
    {
        var order = CreateOpenOrder();
        order.Cancel(DateTime.Now);
        var command = new StartOrderPreparationCommand(CustomerOrderId: 1);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
