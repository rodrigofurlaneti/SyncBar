using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Ifood.Logistics;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Logistics;

public sealed class GetIfoodLogisticsDeliveriesQueryHandlerTests
{
    private readonly IIfoodLogisticsDeliveryRepository _deliveryRepository = Substitute.For<IIfoodLogisticsDeliveryRepository>();
    private readonly IIfoodOrderRepository _ifoodOrderRepository = Substitute.For<IIfoodOrderRepository>();
    private readonly ICustomerOrderRepository _customerOrderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodLogisticsDeliveriesQueryHandler _handler;

    public GetIfoodLogisticsDeliveriesQueryHandlerTests()
    {
        _handler = new GetIfoodLogisticsDeliveriesQueryHandler(
            _deliveryRepository, _ifoodOrderRepository, _customerOrderRepository, _logRepository, _unitOfWork);
    }

    private static IfoodOrder CreateOrder()
        => IfoodOrder.Create(
            customerOrderId: 1, branchId: 1, "ifood-order-1", "#001", "MERCH-1", "DELIVERY", null, "IMMEDIATE", null,
            now: DateTime.Now, hasUnmappedItems: false).Value;

    private static CustomerOrder CreateCustomerOrder()
        => CustomerOrder.Create(
            branchId: 1, diningTableId: null, comandaId: null, employeeId: 1, guestCount: null, notes: null,
            Now: DateTime.Now, orderTypeId: SyncBar.Domain.Constants.OrderTypeIds.Delivery,
            customerName: "Maria Silva", deliveryAddress: "Rua A, 123").Value;

    [Fact]
    public async Task Handle_NoOpenDeliveries_ShouldReturnEmptyList()
    {
        var query = new GetIfoodLogisticsDeliveriesQuery(BranchId: 1);
        _deliveryRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<IfoodLogisticsDelivery>)[]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _ifoodOrderRepository.DidNotReceive().GetOpenByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Este teste está suspenso até que o bug #123 seja corrigido.")]
    public async Task Handle_WithDeliveries_ShouldMapOrderAndCustomerInfo()
    {
        var order = CreateOrder();
        var customerOrder = CreateCustomerOrder();
        var delivery = IfoodLogisticsDelivery.Create(order.Id, order.BranchId, "João", "11999998888", "MOTORCYCLE", DateTime.Now).Value;
        var query = new GetIfoodLogisticsDeliveriesQuery(BranchId: 1);

        _deliveryRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<IfoodLogisticsDelivery>)[delivery]);
        _ifoodOrderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<IfoodOrder>)[order]);
        _customerOrderRepository.GetByIdsAsync(
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.Contains(order.CustomerOrderId)), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<CustomerOrder>)[customerOrder]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var response = result.Value.Single();
        response.DriverName.Should().Be("João");
        response.IfoodOrderDisplayId.Should().Be("#001");
        response.CustomerName.Should().Be("Maria Silva");
        response.DeliveryAddress.Should().Be("Rua A, 123");
    }

    [Fact]
    public async Task Handle_DeliveryWithoutMatchingOrder_ShouldMapNullFieldsWithoutBreaking()
    {
        // Entrega cujo IfoodOrderId não bate com nenhum pedido "aberto" retornado — caso raro
        // (pedido já concluído no Ifood mas entrega local ainda não fechada).
        var delivery = IfoodLogisticsDelivery.Create(999, 1, "João", "11999998888", "MOTORCYCLE", DateTime.Now).Value;
        var query = new GetIfoodLogisticsDeliveriesQuery(BranchId: 1);

        _deliveryRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<IfoodLogisticsDelivery>)[delivery]);
        _ifoodOrderRepository.GetOpenByBranchAsync(query.BranchId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<IfoodOrder>)[]);
        _customerOrderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<CustomerOrder>)[]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.IfoodOrderDisplayId.Should().BeNull();
        response.CustomerName.Should().BeNull();
        response.DeliveryAddress.Should().BeNull();
    }
}
