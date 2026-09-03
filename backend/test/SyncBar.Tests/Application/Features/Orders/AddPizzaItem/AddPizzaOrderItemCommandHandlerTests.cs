using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Orders.AddPizzaItem;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Exceptions;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.AddPizzaItem;

public sealed class AddPizzaOrderItemCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IPizzaConfigurationRepository _pizzaConfigurationRepository = Substitute.For<IPizzaConfigurationRepository>();
    private readonly IProductStockRepository _stockRepository = Substitute.For<IProductStockRepository>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AddPizzaOrderItemCommandHandler _handler;

    public AddPizzaOrderItemCommandHandlerTests()
    {
        _handler = new AddPizzaOrderItemCommandHandler(
            _orderRepository, _productRepository, _pizzaConfigurationRepository, _stockRepository,
            _printingService, TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateTableOrder()
        => CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;

    private static CustomerOrder CreateComandaOrder(decimal? creditLimitAmount)
        => CustomerOrder.Create(1, null, 20, 1, null, null, DateTime.Now, creditLimitAmount: creditLimitAmount).Value;

    private static Product CreateProduct(bool active = true)
    {
        var product = Product.Create(1, 1, 1, "Pizza Grande", null, null, 0m, null, false, null).Value;
        if (!active) product.Deactivate();
        return product;
    }

    // Configuração com 1 tamanho aceitando até 2 frações e 2 sabores precificados nesse tamanho.
    private static PizzaConfiguration CreateConfiguration(
        long productId, out long sizeId, out long flavorAId, out long flavorBId,
        decimal priceA = 40m, decimal priceB = 50m, int acceptedFractions = 2,
        long? crustId = null, decimal crustExtra = 0m, long? edgeId = null, decimal edgeExtra = 0m)
    {
        var configuration = PizzaConfiguration.Create(productId).Value;
        var size = configuration.AddSize("Grande", slices: 8, acceptedFractions, displayOrder: 0).Value;
        sizeId = size.Id;
        flavorAId = 101;
        flavorBId = 102;
        configuration.SetFlavorPrice(flavorAId, sizeId, priceA);
        configuration.SetFlavorPrice(flavorBId, sizeId, priceB);
        return configuration;
    }

    private void SetupOrder(CustomerOrder order, long orderId = 1)
        => _orderRepository.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

    private void SetupProduct(Product product)
        => _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

    private void SetupConfiguration(long productId, PizzaConfiguration configuration)
        => _pizzaConfigurationRepository.GetByProductIdAsync(productId, Arg.Any<CancellationToken>()).Returns(configuration);

    [Fact]
    public async Task Handle_NoFlavorsSelected_ShouldReturnFailureWithoutTouchingRepositories()
    {
        var command = new AddPizzaOrderItemCommand(
            CustomerOrderId: 1, ProductId: 1, Quantity: 1, Notes: null, EmployeeId: null,
            PizzaSizeId: 1, PizzaCrustId: null, PizzaEdgeId: null, PizzaFlavorIds: []);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NoFlavorsSelected");
        await _orderRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((CustomerOrder?)null);
        var command = new AddPizzaOrderItemCommand(
            CustomerOrderId: 1, ProductId: 1, Quantity: 1, Notes: null, EmployeeId: null,
            PizzaSizeId: 1, PizzaCrustId: null, PizzaEdgeId: null, PizzaFlavorIds: [101]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductInactive_ShouldReturnFailure()
    {
        SetupOrder(CreateTableOrder());
        var product = CreateProduct(active: false);
        SetupProduct(product);
        var command = new AddPizzaOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            PizzaSizeId: 1, PizzaCrustId: null, PizzaEdgeId: null, PizzaFlavorIds: [101]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_PizzaConfigurationNotFound_ShouldReturnFailure()
    {
        SetupOrder(CreateTableOrder());
        var product = CreateProduct();
        SetupProduct(product);
        _pizzaConfigurationRepository.GetByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns((PizzaConfiguration?)null);
        var command = new AddPizzaOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            PizzaSizeId: 1, PizzaCrustId: null, PizzaEdgeId: null, PizzaFlavorIds: [101]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_InvalidPizzaSize_ShouldReturnFailurePropagatedFromConfiguration()
    {
        SetupOrder(CreateTableOrder());
        var product = CreateProduct();
        SetupProduct(product);
        var configuration = CreateConfiguration(product.Id, out _, out var flavorA, out _);
        SetupConfiguration(product.Id, configuration);
        var command = new AddPizzaOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            PizzaSizeId: 9999, PizzaCrustId: null, PizzaEdgeId: null, PizzaFlavorIds: [flavorA]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.SizeNotFound");
    }

    [Fact]
    public async Task Handle_ComandaCreditLimitExceeded_ShouldReturnFailure()
    {
        var order = CreateComandaOrder(creditLimitAmount: 30m);
        SetupOrder(order);
        var product = CreateProduct();
        SetupProduct(product);
        var configuration = CreateConfiguration(product.Id, out var sizeId, out var flavorA, out _, priceA: 60m);
        SetupConfiguration(product.Id, configuration);
        var command = new AddPizzaOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            PizzaSizeId: sizeId, PizzaCrustId: null, PizzaEdgeId: null, PizzaFlavorIds: [flavorA]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.LimitExceeded");
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_InsufficientStock_ShouldReturnFailure()
    {
        SetupOrder(CreateTableOrder());
        var product = CreateProduct();
        SetupProduct(product);
        var configuration = CreateConfiguration(product.Id, out var sizeId, out var flavorA, out _);
        SetupConfiguration(product.Id, configuration);
        _stockRepository.GetByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new ProductStock(product.Id, initialBalance: 0m, minimumQuantity: 0m));
        var command = new AddPizzaOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            PizzaSizeId: sizeId, PizzaCrustId: null, PizzaEdgeId: null, PizzaFlavorIds: [flavorA]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Stock.Insufficient");
    }

    [Fact]
    public async Task Handle_ConcurrencyExceptionOnCommit_ShouldReturnFailureAndCommitTwice()
    {
        SetupOrder(CreateTableOrder());
        var product = CreateProduct();
        SetupProduct(product);
        var configuration = CreateConfiguration(product.Id, out var sizeId, out var flavorA, out _);
        SetupConfiguration(product.Id, configuration);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new ConcurrencyException("Estoque alterado concorrentemente."));
        var command = new AddPizzaOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            PizzaSizeId: sizeId, PizzaCrustId: null, PizzaEdgeId: null, PizzaFlavorIds: [flavorA]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Stock.Concurrency");
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldAddPizzaItemWithMostExpensiveFlavorPriceAndCommitTwice()
    {
        var order = CreateTableOrder();
        SetupOrder(order);
        var product = CreateProduct();
        SetupProduct(product);
        var configuration = CreateConfiguration(product.Id, out var sizeId, out var flavorA, out var flavorB, priceA: 40m, priceB: 55m);
        SetupConfiguration(product.Id, configuration);
        var command = new AddPizzaOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 2, Notes: "Bem assada", EmployeeId: 7,
            PizzaSizeId: sizeId, PizzaCrustId: null, PizzaEdgeId: null, PizzaFlavorIds: [flavorA, flavorB]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().ContainSingle(); // sempre 1 OrderItem por pizza, mesmo fracionada
        var item = order.Items.First();
        item.UnitPrice.Should().Be(55m); // sabor mais caro entre os escolhidos
        item.Quantity.Should().Be(2);
        item.PizzaSizeId.Should().Be(sizeId);
        item.PizzaFlavors.Should().HaveCount(2);
        await _printingService.Received(1).PrintOrderItemsAsync(order.Id, Arg.Is<IReadOnlyCollection<long>>(ids => ids.Contains(item.Id)), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithCrustAndEdge_ShouldAddExtraPricesToUnitPrice()
    {
        var order = CreateTableOrder();
        SetupOrder(order);
        var product = CreateProduct();
        SetupProduct(product);
        var configuration = CreateConfiguration(product.Id, out var sizeId, out var flavorA, out _, priceA: 40m);
        var crust = configuration.AddCrust("Catupiry", extraPrice: 8m, displayOrder: 0).Value;
        var edge = configuration.AddEdge("Cheddar", extraPrice: 5m, displayOrder: 0).Value;
        SetupConfiguration(product.Id, configuration);
        var command = new AddPizzaOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            PizzaSizeId: sizeId, PizzaCrustId: crust.Id, PizzaEdgeId: edge.Id, PizzaFlavorIds: [flavorA]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.First().UnitPrice.Should().Be(53m); // 40 (sabor) + 8 (borda) + 5 (recheio de borda)
    }
}
