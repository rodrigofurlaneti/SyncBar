using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Orders;

public sealed class GetIfoodOrdersQueryHandlerTests
{
    private const long BranchId = 10;

    private readonly IIfoodOrderRepository _IfoodOrderRepository = Substitute.For<IIfoodOrderRepository>();
    private readonly ICustomerOrderRepository _customerOrderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private GetIfoodOrdersQueryHandler CreateSut() =>
        new(_IfoodOrderRepository, _customerOrderRepository, _logRepository, _unitOfWork);

    private static IfoodOrder CreateIfoodOrder(string IfoodOrderId, DateTime createdAt) =>
        IfoodOrder.Create(
            customerOrderId: 0, branchId: BranchId, IfoodOrderId: IfoodOrderId, displayId: "001",
            merchantId: "merchant-1", IfoodOrderType: "DELIVERY", deliveredBy: "Ifood", orderTiming: "IMMEDIATE",
            preparationStartDateTime: null, now: createdAt, hasUnmappedItems: false).Value;

    [Fact]
    public async Task Handle_WhenBranchHasNoOpenOrders_ShouldSucceedWithEmptyList()
    {
        _IfoodOrderRepository.GetOpenByBranchAsync(BranchId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<IfoodOrder>)Array.Empty<IfoodOrder>());
        var sut = CreateSut();

        var result = await sut.Handle(new GetIfoodOrdersQuery(BranchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Não deve nem consultar CustomerOrder quando não há pedido Ifood aberto.
        await _customerOrderRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCustomerOrderIsMissing_ShouldFallBackToDefaultCustomerName()
    {
        var IfoodOrder = CreateIfoodOrder("Ifood-1", DateTime.Now);
        _IfoodOrderRepository.GetOpenByBranchAsync(BranchId, Arg.Any<CancellationToken>())
            .Returns(new List<IfoodOrder> { IfoodOrder });
        _customerOrderRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<CustomerOrder>)Array.Empty<CustomerOrder>());
        var sut = CreateSut();

        var result = await sut.Handle(new GetIfoodOrdersQuery(BranchId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.CustomerName.Should().Be("Cliente Ifood");
        response.CustomerPhone.Should().BeNull();
        response.DeliveryAddress.Should().BeNull();
        response.TotalAmount.Should().Be(0m);
    }
}
