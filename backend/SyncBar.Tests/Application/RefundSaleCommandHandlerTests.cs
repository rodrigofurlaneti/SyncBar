using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Billing.RefundSale;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class RefundSaleCommandHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly ICashMovementRepository _cashMovementRepository = Substitute.For<ICashMovementRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RefundSaleCommandHandler CreateHandler()
        => new(_saleRepository, _orderRepository, _cashSessionRepository, _cashMovementRepository,
            _diningTableRepository, _comandaRepository, _unitOfWork);

    private static (Sale sale, CustomerOrder order) PaidScenario()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(1, 100m, 1, null, null);
        order.Close(0.10m);
        order.MarkAsPaid();
        var sale = Sale.Create(1, 1, 7, 1, 5, 100m, 0m, 10m).Value;
        return (sale, order);
    }

    [Fact]
    public async Task Handle_WithOpenSession_ShouldDeactivateSaleAndReopenOrder()
    {
        var (sale, order) = PaidScenario();
        _saleRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(sale);
        _cashSessionRepository.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(CashSession.Open(1, 1, 100m).Value);
        _orderRepository.GetByIdForUpdateAsync(sale.CustomerOrderId, Arg.Any<CancellationToken>()).Returns(order);

        var result = await CreateHandler().Handle(
            new RefundSaleCommand(1, 2, "cobrado errado"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sale.IsActive.Should().BeFalse();
        order.OrderStatusId.Should().Be(OrderStatusIds.AguardandoPagamento);
        await _cashMovementRepository.Received(1).AddAsync(
            Arg.Is<CashMovement>(m => m.CashMovementTypeId == CashMovementTypeIds.EstornoVenda),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithClosedSession_ShouldFail()
    {
        var (sale, _) = PaidScenario();
        _saleRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(sale);
        var closed = CashSession.Open(1, 1, 100m).Value;
        closed.Close(1, 100m, 100m);
        _cashSessionRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(closed);

        var result = await CreateHandler().Handle(
            new RefundSaleCommand(1, 2, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sale.SessionClosed");
        sale.IsActive.Should().BeTrue();
    }
}
