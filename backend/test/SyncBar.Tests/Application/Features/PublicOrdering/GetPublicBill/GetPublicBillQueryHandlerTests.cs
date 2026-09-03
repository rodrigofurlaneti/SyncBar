using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.PublicOrdering.GetPublicBill;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.PublicOrdering.GetPublicBill;

public sealed class GetPublicBillQueryHandlerTests
{
    private const long BranchId = 1;

    private readonly IDiningTableRepository _tableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetPublicBillQueryHandler _handler;

    public GetPublicBillQueryHandlerTests()
    {
        _handler = new GetPublicBillQueryHandler(
            _tableRepository, _orderRepository, _productRepository, _logRepository, _unitOfWork);
    }

    private static DiningTable MakeTable(int number = 7) => DiningTable.Create(BranchId, 1, number, 4).Value;

    private static Product MakeNamedProduct(long id, string name)
    {
        var product = Product.Create(1, 1, 1, name, null, null, 0m, null, false, null).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(product, id);
        return product;
    }

    [Fact]
    public async Task Handle_InvalidToken_ShouldReturnFailure()
    {
        var query = new GetPublicBillQuery(Guid.NewGuid());
        _tableRepository.GetByQrTokenAsync(query.Token, Arg.Any<CancellationToken>()).Returns((DiningTable?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.InvalidToken");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    // O handler NÃO valida table.IsActive (só a ausência/invalidade do token) — diferente de
    // AddPublicOrderItemCommandHandler. Comportamento real confirmado na leitura do código-fonte;
    // fixado aqui como proteção de regressão, não como suposição.
    [Fact]
    public async Task Handle_InactiveTableButValidToken_ShouldStillSucceed()
    {
        var table = MakeTable();
        table.Deactivate();
        var order = CustomerOrder.Create(BranchId, table.Id, null, 10, null, "Pedido via QR Code", DateTime.Now).Value;
        var query = new GetPublicBillQuery(Guid.NewGuid());
        _tableRepository.GetByQrTokenAsync(query.Token, Arg.Any<CancellationToken>()).Returns(table);
        _orderRepository.GetOpenByTableAsync(table.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoOpenOrderForTable_ShouldReturnFailure()
    {
        var table = MakeTable();
        var query = new GetPublicBillQuery(Guid.NewGuid());
        _tableRepository.GetByQrTokenAsync(query.Token, Arg.Any<CancellationToken>()).Returns(table);
        _orderRepository.GetOpenByTableAsync(table.Id, Arg.Any<CancellationToken>()).Returns((CustomerOrder?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnBillWithMappedItemsAndTotals()
    {
        var table = MakeTable(number: 12);
        var order = CustomerOrder.Create(BranchId, table.Id, null, 10, null, "Pedido via QR Code", DateTime.Now).Value;
        order.AddItem(100, 20m, 2, "Sem gelo", null, DateTime.Now);
        order.AddItem(200, 15m, 1, null, null, DateTime.Now);
        var productBurger = MakeNamedProduct(100, "X-Burger");
        var productSoda = MakeNamedProduct(200, "Refrigerante");
        var query = new GetPublicBillQuery(Guid.NewGuid());
        _tableRepository.GetByQrTokenAsync(query.Token, Arg.Any<CancellationToken>()).Returns(table);
        _orderRepository.GetOpenByTableAsync(table.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { productBurger, productSoda });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var bill = result.Value;
        bill.OrderId.Should().Be(order.Id);
        bill.TableNumber.Should().Be("12");
        bill.Status.Should().Be(order.OrderStatusId.ToString());
        bill.SubtotalAmount.Should().Be(order.SubtotalAmount);
        bill.TotalAmount.Should().Be(order.TotalAmount);
        bill.Items.Should().HaveCount(2);
        bill.Items.Should().Contain(i => i.ProductName == "X-Burger" && i.Quantity == 2 && i.UnitPrice == 20m && i.Notes == "Sem gelo");
        bill.Items.Should().Contain(i => i.ProductName == "Refrigerante" && i.Quantity == 1 && i.UnitPrice == 15m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveItem_ShouldBeExcludedFromBill()
    {
        var table = MakeTable();
        var order = CustomerOrder.Create(BranchId, table.Id, null, 10, null, "Pedido via QR Code", DateTime.Now).Value;
        order.AddItem(100, 20m, 1, null, null, DateTime.Now);
        order.Items.First().Deactivate(DateTime.Now);
        var query = new GetPublicBillQuery(Guid.NewGuid());
        _tableRepository.GetByQrTokenAsync(query.Token, Arg.Any<CancellationToken>()).Returns(table);
        _orderRepository.GetOpenByTableAsync(table.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ItemWithProductMissingFromCatalog_ShouldUseFallbackName()
    {
        var table = MakeTable();
        var order = CustomerOrder.Create(BranchId, table.Id, null, 10, null, "Pedido via QR Code", DateTime.Now).Value;
        order.AddItem(999, 10m, 1, null, null, DateTime.Now);
        var query = new GetPublicBillQuery(Guid.NewGuid());
        _tableRepository.GetByQrTokenAsync(query.Token, Arg.Any<CancellationToken>()).Returns(table);
        _orderRepository.GetOpenByTableAsync(table.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.ProductName == "Produto Desconhecido");
    }

    [Fact]
    public async Task Handle_OrderWithNoItems_ShouldReturnEmptyItemsAndZeroTotals()
    {
        var table = MakeTable();
        var order = CustomerOrder.Create(BranchId, table.Id, null, 10, null, "Pedido via QR Code", DateTime.Now).Value;
        var query = new GetPublicBillQuery(Guid.NewGuid());
        _tableRepository.GetByQrTokenAsync(query.Token, Arg.Any<CancellationToken>()).Returns(table);
        _orderRepository.GetOpenByTableAsync(table.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.SubtotalAmount.Should().Be(0m);
        result.Value.TotalAmount.Should().Be(0m);
    }
}
