using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Preparation.GetQueue;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class GetPreparationQueueQueryHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();

    private GetPreparationQueueQueryHandler CreateHandler()
        => new(_orderRepository, _productRepository, _diningTableRepository, _comandaRepository, _employeeRepository);

    private static T WithId<T>(T entity, long id) where T : SyncBar.Domain.Primitives.Entity
    {
        typeof(SyncBar.Domain.Primitives.Entity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    [Fact]
    public async Task Handle_ShouldBuildTicketsWithKitchenTimeAndBarTolerance()
    {
        var order = WithId(CustomerOrder.Create(1, 10, null, 1, null, null).Value, 77);
        order.AddItem(1, 14.90m, 2, null, 5);      // lancado pelo funcionario 5 (Maria)
        order.AddItem(2, 32m, 1, "sem sal", null); // sem responsavel no item → garcom do pedido (1, Joao)
        _orderRepository.GetOpenByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<CustomerOrder> { order });

        var beer = WithId(Product.Create(1, 1, 1, "Cerveja", null, null, 14.90m, 6m, true, null).Value, 1);
        var fries = WithId(Product.Create(1, 4, 7, "Batata Frita", null, null, 32m, 10m, false, 20).Value, 2);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { beer, fries });

        var table = WithId(DiningTable.Create(1, TableStatusIds.Ocupada, 10, 4).Value, 10);
        _diningTableRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<DiningTable> { table });

        var joao = WithId(Employee.Create(1, 2, "João", "11111111111", null, null, DateTime.UtcNow, null, null).Value, 1);
        var maria = WithId(Employee.Create(1, 2, "Maria", "22222222222", null, null, DateTime.UtcNow, null, null).Value, 5);
        _employeeRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { joao, maria });

        var result = await CreateHandler().Handle(new GetPreparationQueueQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var ticket = result.Value.Single();
        ticket.CustomerOrderId.Should().Be(77);
        ticket.TableNumber.Should().Be(10);
        ticket.Items.Should().HaveCount(2);

        var beerItem = ticket.Items.Single(i => i.ProductId == 1);
        beerItem.IsBarItem.Should().BeTrue();
        beerItem.LimitMinutes.Should().Be(5);
        beerItem.RequestedBy.Should().Be("Maria");

        var friesItem = ticket.Items.Single(i => i.ProductId == 2);
        friesItem.IsBarItem.Should().BeFalse();
        friesItem.LimitMinutes.Should().Be(20);
        friesItem.Notes.Should().Be("sem sal");
        friesItem.RequestedBy.Should().Be("João"); // herdado do garcom do pedido
    }

    [Fact]
    public async Task Handle_OrderWithOnlyDeliveredItems_ShouldNotAppear()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(1, 10m, 1, null, null);
        var item = order.Items.First();
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.EnviadoCozinha);
        order.UpdateItemStatus(item.Id, OrderItemStatusIds.Entregue);

        _orderRepository.GetOpenByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<CustomerOrder> { order });
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());
        _diningTableRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<DiningTable>());
        _employeeRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<Employee>());

        var result = await CreateHandler().Handle(new GetPreparationQueueQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
