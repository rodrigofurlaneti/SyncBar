using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.PublicOrdering.GetPublicComandaBill;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.PublicOrdering.GetPublicComandaBill;

public sealed class GetPublicComandaBillQueryHandlerTests
{
    private const long BranchId = 1;
    private const string ComandaCode = "001";

    private readonly IDiningTableRepository _tableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetPublicComandaBillQueryHandler _handler;

    public GetPublicComandaBillQueryHandlerTests()
    {
        _handler = new GetPublicComandaBillQueryHandler(
            _tableRepository, _comandaRepository, _orderRepository, _productRepository, _logRepository, _unitOfWork);
    }

    private static DiningTable MakeTable(int number = 7) => DiningTable.Create(BranchId, 1, number, 4).Value;

    private static Comanda MakeComanda(string code = ComandaCode) => Comanda.Create(BranchId, 1, code).Value;

    private static Product MakeNamedProduct(long id, string name)
    {
        var product = Product.Create(1, 1, 1, name, null, null, 0m, null, false, null).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(product, id);
        return product;
    }

    [Fact]
    public async Task Handle_InvalidToken_ShouldReturnFailure()
    {
        var query = new GetPublicComandaBillQuery(Guid.NewGuid(), ComandaCode);
        _tableRepository.GetByQrTokenAsync(query.TableToken, Arg.Any<CancellationToken>()).Returns((DiningTable?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.InvalidToken");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComandaNotFound_ShouldReturnFailure()
    {
        var table = MakeTable();
        var query = new GetPublicComandaBillQuery(Guid.NewGuid(), "999");
        _tableRepository.GetByQrTokenAsync(query.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, "999", Arg.Any<CancellationToken>()).Returns((Comanda?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveComanda_ShouldReturnFailure()
    {
        var table = MakeTable();
        var comanda = MakeComanda();
        comanda.Deactivate();
        var query = new GetPublicComandaBillQuery(Guid.NewGuid(), ComandaCode);
        _tableRepository.GetByQrTokenAsync(query.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoOpenOrderForComanda_ShouldReturnFailure()
    {
        var table = MakeTable();
        var comanda = MakeComanda();
        var query = new GetPublicComandaBillQuery(Guid.NewGuid(), ComandaCode);
        _tableRepository.GetByQrTokenAsync(query.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);
        _orderRepository.GetOpenByComandaAsync(comanda.Id, Arg.Any<CancellationToken>()).Returns((CustomerOrder?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnBillWithMappedItemsTotalsAndCreditLimit()
    {
        var table = MakeTable();
        var comanda = MakeComanda();
        var order = CustomerOrder.Create(BranchId, null, comanda.Id, 10, null, "Mesa 7 — Pedido via QR Code", DateTime.Now, 500m).Value;
        order.AddItem(100, 20m, 2, "Sem gelo", null, DateTime.Now);
        var product = MakeNamedProduct(100, "X-Burger");
        var query = new GetPublicComandaBillQuery(Guid.NewGuid(), ComandaCode);
        _tableRepository.GetByQrTokenAsync(query.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);
        _orderRepository.GetOpenByComandaAsync(comanda.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var bill = result.Value;
        bill.OrderId.Should().Be(order.Id);
        bill.ComandaCode.Should().Be(ComandaCode);
        bill.Status.Should().Be(order.OrderStatusId.ToString());
        bill.SubtotalAmount.Should().Be(order.SubtotalAmount);
        bill.TotalAmount.Should().Be(order.TotalAmount);
        bill.CreditLimitAmount.Should().Be(500m);
        bill.Items.Should().ContainSingle();
        bill.Items.First().ProductName.Should().Be("X-Burger");
        bill.Items.First().Quantity.Should().Be(2);
        bill.Items.First().UnitPrice.Should().Be(20m);
        bill.Items.First().Notes.Should().Be("Sem gelo");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemWithProductMissingFromCatalog_ShouldUseFallbackName()
    {
        var table = MakeTable();
        var comanda = MakeComanda();
        var order = CustomerOrder.Create(BranchId, null, comanda.Id, 10, null, "Mesa 7 — Pedido via QR Code", DateTime.Now, 500m).Value;
        order.AddItem(999, 10m, 1, null, null, DateTime.Now);
        var query = new GetPublicComandaBillQuery(Guid.NewGuid(), ComandaCode);
        _tableRepository.GetByQrTokenAsync(query.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);
        _orderRepository.GetOpenByComandaAsync(comanda.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Fallback distinto do usado em GetPublicBillQueryHandler ("Produto Desconhecido") —
        // aqui é só "Produto", confirmado lendo o código-fonte do handler.
        result.Value.Items.Should().ContainSingle(i => i.ProductName == "Produto");
    }

    [Fact]
    public async Task Handle_InactiveItem_ShouldBeExcludedFromBill()
    {
        var table = MakeTable();
        var comanda = MakeComanda();
        var order = CustomerOrder.Create(BranchId, null, comanda.Id, 10, null, "Mesa 7 — Pedido via QR Code", DateTime.Now, 500m).Value;
        order.AddItem(100, 20m, 1, null, null, DateTime.Now);
        order.Items.First().Deactivate(DateTime.Now);
        var query = new GetPublicComandaBillQuery(Guid.NewGuid(), ComandaCode);
        _tableRepository.GetByQrTokenAsync(query.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);
        _orderRepository.GetOpenByComandaAsync(comanda.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OrderWithNoItems_ShouldReturnEmptyItemsAndZeroTotals()
    {
        var table = MakeTable();
        var comanda = MakeComanda();
        var order = CustomerOrder.Create(BranchId, null, comanda.Id, 10, null, "Mesa 7 — Pedido via QR Code", DateTime.Now, 500m).Value;
        var query = new GetPublicComandaBillQuery(Guid.NewGuid(), ComandaCode);
        _tableRepository.GetByQrTokenAsync(query.TableToken, Arg.Any<CancellationToken>()).Returns(table);
        _comandaRepository.GetByCodeAsync(table.BranchId, ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);
        _orderRepository.GetOpenByComandaAsync(comanda.Id, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.SubtotalAmount.Should().Be(0m);
        result.Value.TotalAmount.Should().Be(0m);
    }
}
