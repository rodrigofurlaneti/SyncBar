using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Orders;

public sealed class GetIFoodOrdersQueryHandlerTests
{
    private const long BranchId = 10;

    private readonly IIFoodOrderRepository _ifoodOrderRepository = Substitute.For<IIFoodOrderRepository>();
    private readonly ICustomerOrderRepository _customerOrderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private GetIFoodOrdersQueryHandler CreateSut() =>
        new(_ifoodOrderRepository, _customerOrderRepository, _logRepository, _unitOfWork);

    private static IFoodOrder CreateIFoodOrder(string ifoodOrderId, DateTime createdAt) =>
        IFoodOrder.Create(
            customerOrderId: 0, branchId: BranchId, ifoodOrderId: ifoodOrderId, displayId: "001",
            merchantId: "merchant-1", ifoodOrderType: "DELIVERY", deliveredBy: "IFOOD", orderTiming: "IMMEDIATE",
            preparationStartDateTime: null, now: createdAt, hasUnmappedItems: false).Value;

    [Fact]
    public async Task Handle_WhenBranchHasNoOpenOrders_ShouldSucceedWithEmptyList()
    {
        _ifoodOrderRepository.GetOpenByBranchAsync(BranchId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<IFoodOrder>)Array.Empty<IFoodOrder>());
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodOrdersQuery(BranchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Não deve nem consultar CustomerOrder quando não há pedido iFood aberto.
        await _customerOrderRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCustomerOrderIsMissing_ShouldFallBackToDefaultCustomerName()
    {
        var ifoodOrder = CreateIFoodOrder("ifood-1", DateTime.Now);
        _ifoodOrderRepository.GetOpenByBranchAsync(BranchId, Arg.Any<CancellationToken>())
            .Returns(new List<IFoodOrder> { ifoodOrder });
        _customerOrderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<CustomerOrder>)Array.Empty<CustomerOrder>());
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodOrdersQuery(BranchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.CustomerName.Should().Be("Cliente iFood");
        response.CustomerPhone.Should().BeNull();
        response.DeliveryAddress.Should().BeNull();
        response.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WhenCustomerOrderMatches_ShouldMapRealCustomerData()
    {
        var ifoodOrder = CreateIFoodOrder("ifood-1", DateTime.Now);
        var customerOrder = CustomerOrder.Create(
            branchId: BranchId, diningTableId: null, comandaId: null, employeeId: 1, guestCount: null, notes: null,
            Now: DateTime.Now, orderTypeId: OrderTypeIds.Delivery,
            customerName: "Maria Silva", customerPhone: "11999999999", deliveryAddress: "Rua das Flores, 100").Value;
        customerOrder.AddItem(productId: 1, unitPrice: 42.50m, quantity: 1, notes: null, employeeId: null, Now: DateTime.Now);

        _ifoodOrderRepository.GetOpenByBranchAsync(BranchId, Arg.Any<CancellationToken>())
            .Returns(new List<IFoodOrder> { ifoodOrder });
        _customerOrderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CustomerOrder> { customerOrder });
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodOrdersQuery(BranchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.CustomerName.Should().Be("Maria Silva");
        response.CustomerPhone.Should().Be("11999999999");
        response.DeliveryAddress.Should().Be("Rua das Flores, 100");
        response.TotalAmount.Should().Be(42.50m);
        response.IFoodOrderId.Should().Be("ifood-1");
    }

    [Fact]
    public async Task Handle_WithMultipleOrders_ShouldOrderByCreatedAtAscending()
    {
        var older = CreateIFoodOrder("ifood-older", new DateTime(2026, 1, 1));
        var newer = CreateIFoodOrder("ifood-newer", new DateTime(2026, 1, 2));
        _ifoodOrderRepository.GetOpenByBranchAsync(BranchId, Arg.Any<CancellationToken>())
            .Returns(new List<IFoodOrder> { newer, older });
        _customerOrderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<CustomerOrder>)Array.Empty<CustomerOrder>());
        var sut = CreateSut();

        var result = await sut.Handle(new GetIFoodOrdersQuery(BranchId), CancellationToken.None);

        result.Value.Select(x => x.IFoodOrderId).Should().ContainInOrder("ifood-older", "ifood-newer");
    }
}
