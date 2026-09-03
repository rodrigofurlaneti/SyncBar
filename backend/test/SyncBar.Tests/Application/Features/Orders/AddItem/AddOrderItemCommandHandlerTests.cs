using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Exceptions;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.AddItem;

public sealed class AddOrderItemCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IPromotionRepository _promotionRepository = Substitute.For<IPromotionRepository>();
    private readonly IProductStockRepository _stockRepository = Substitute.For<IProductStockRepository>();
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AddOrderItemCommandHandler _handler;

    public AddOrderItemCommandHandlerTests()
    {
        // Nenhuma promoção por padrão — testes de promoção sobrescrevem explicitamente.
        _promotionRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<Promotion>());

        // TimeProvider.GetLocalNow() não é mockado aqui (mesma convenção de
        // OpenOrderCommandHandlerTests) — usa TimeProvider.System real. Os testes de
        // promoção usam uma janela que cobre o dia inteiro para não depender do horário
        // exato de execução do teste.
        _handler = new AddOrderItemCommandHandler(
            _orderRepository, _productRepository, _promotionRepository, _stockRepository,
            _productComplementGroupRepository, _complementGroupRepository, _complementItemRepository,
            _printingService, TimeProvider.System, _logRepository, _unitOfWork);
    }

    private static CustomerOrder CreateTableOrder()
        => CustomerOrder.Create(1, 10, null, 1, null, null, DateTime.Now).Value;

    private static CustomerOrder CreateComandaOrder(decimal? creditLimitAmount)
        => CustomerOrder.Create(1, null, 20, 1, null, null, DateTime.Now, creditLimitAmount: creditLimitAmount).Value;

    private static Product CreateProduct(decimal salePrice = 20m, bool active = true)
    {
        var product = Product.Create(1, 1, 1, "X-Salada", null, null, salePrice, null, false, null).Value;
        if (!active) product.Deactivate();
        return product;
    }

    private static ComplementItem CreateComplementItem(long id, long? linkedProductId = null)
    {
        var item = ComplementItem.Create(1, "Bacon extra", linkedProductId).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, id);
        return item;
    }

    private void SetupOrder(CustomerOrder order, long orderId = 1)
        => _orderRepository.GetByIdForUpdateAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

    private void SetupProduct(Product product)
        => _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        var command = new AddOrderItemCommand(CustomerOrderId: 1, ProductId: 1, Quantity: 1, Notes: null, EmployeeId: null);
        _orderRepository.GetByIdForUpdateAsync(command.CustomerOrderId, Arg.Any<CancellationToken>())
            .Returns((CustomerOrder?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderNotOpen_ShouldReturnFailure()
    {
        // IsActive (flag de soft-delete) continua true após Cancel() — só OrderStatusId muda —
        // então o pedido passa pela checagem de "encontrado" e falha depois, dentro de
        // CustomerOrder.AddItem, com o código real de "pedido não está aberto".
        var order = CreateTableOrder();
        order.Cancel(DateTime.Now);
        SetupOrder(order);
        var product = CreateProduct();
        SetupProduct(product);
        var command = new AddOrderItemCommand(CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        SetupOrder(CreateTableOrder());
        var command = new AddOrderItemCommand(CustomerOrderId: 1, ProductId: 99, Quantity: 1, Notes: null, EmployeeId: null);
        _productRepository.GetByIdAsync(command.ProductId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ProductInactive_ShouldReturnFailure()
    {
        SetupOrder(CreateTableOrder());
        var product = CreateProduct(active: false);
        SetupProduct(product);
        var command = new AddOrderItemCommand(CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ComplementGroupNotLinkedToProduct_ShouldReturnFailure()
    {
        SetupOrder(CreateTableOrder());
        var product = CreateProduct();
        SetupProduct(product);
        _productComplementGroupRepository.GetByProductAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup>()); // nenhum grupo vinculado
        var command = new AddOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            Complements: [new OrderItemComplementSelection(ComplementGroupId: 5, ComplementId: 1)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.ComplementGroupNotAvailable");
    }

    [Fact]
    public async Task Handle_ComplementGroupInactive_ShouldReturnFailure()
    {
        SetupOrder(CreateTableOrder());
        var product = CreateProduct();
        SetupProduct(product);
        var link = ProductComplementGroup.Create(product.Id, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        var group = ComplementGroup.Create(1, "Adicionais", ComplementGroupTypeIds.Ingredientes, 0, 5).Value;
        group.AddComplement(complementItemId: 1, extraPrice: 5m);
        group.Deactivate();
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(group);
        var command = new AddOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            Complements: [new OrderItemComplementSelection(ComplementGroupId: 5, ComplementId: 1)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_ComplementNotFoundInGroup_ShouldReturnFailure()
    {
        SetupOrder(CreateTableOrder());
        var product = CreateProduct();
        SetupProduct(product);
        var link = ProductComplementGroup.Create(product.Id, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        var group = ComplementGroup.Create(1, "Adicionais", ComplementGroupTypeIds.Ingredientes, 0, 5).Value;
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(group);
        var command = new AddOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            Complements: [new OrderItemComplementSelection(ComplementGroupId: 5, ComplementId: 999)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.ComplementNotFound");
    }

    [Fact]
    public async Task Handle_ComandaCreditLimitExceeded_ShouldReturnFailure()
    {
        var order = CreateComandaOrder(creditLimitAmount: 50m);
        SetupOrder(order);
        var product = CreateProduct(salePrice: 30m);
        SetupProduct(product);
        var command = new AddOrderItemCommand(CustomerOrderId: 1, ProductId: product.Id, Quantity: 2, Notes: null, EmployeeId: null);

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
        _stockRepository.GetByProductIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new ProductStock(product.Id, initialBalance: 2m, minimumQuantity: 0m));
        var command = new AddOrderItemCommand(CustomerOrderId: 1, ProductId: product.Id, Quantity: 5, Notes: null, EmployeeId: null);

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
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new ConcurrencyException("Estoque alterado concorrentemente."));
        var command = new AddOrderItemCommand(CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Stock.Concurrency");
        // A chamada explícita do handler falha com ConcurrencyException (é engolida e vira
        // Result.Failure) e depois a base ainda tenta comitar o log — 2 tentativas de commit.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestNoComplements_ShouldAddItemDeductStockAndCommitTwice()
    {
        var order = CreateTableOrder();
        SetupOrder(order);
        var product = CreateProduct(salePrice: 25m);
        SetupProduct(product);
        var stock = new ProductStock(product.Id, initialBalance: 10m, minimumQuantity: 0m);
        _stockRepository.GetByProductIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(stock);
        var command = new AddOrderItemCommand(CustomerOrderId: 1, ProductId: product.Id, Quantity: 3, Notes: "Sem cebola", EmployeeId: 7);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().ContainSingle();
        var item = order.Items.First();
        item.UnitPrice.Should().Be(25m);
        item.Quantity.Should().Be(3);
        item.EmployeeId.Should().Be(7);
        stock.CurrentBalance.Should().Be(7m);
        _stockRepository.Received(1).AddMovement(Arg.Is<StockMovement>(m => m.Quantity == -3m && m.OrderItemId == item.Id));
        await _printingService.Received(1).PrintOrderItemsAsync(order.Id, Arg.Is<IReadOnlyCollection<long>>(ids => ids.Contains(item.Id)), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithComplements_ShouldApplyComplementsOnlyToPrimaryItem()
    {
        var order = CreateTableOrder();
        SetupOrder(order);
        var product = CreateProduct();
        SetupProduct(product);
        var link = ProductComplementGroup.Create(product.Id, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        var group = ComplementGroup.Create(1, "Adicionais", ComplementGroupTypeIds.Ingredientes, 0, 5).Value;
        var complement = group.AddComplement(complementItemId: 10, extraPrice: 4m).Value;
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(group);
        var command = new AddOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            Complements: [new OrderItemComplementSelection(ComplementGroupId: 5, ComplementId: complement.Id)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().ContainSingle();
        var item = order.Items.First();
        item.Complements.Should().ContainSingle(c => c.ComplementId == complement.Id && c.UnitPriceCharged == 4m);
    }

    [Fact]
    public async Task Handle_ActivePromotionDesconto_ShouldApplyDiscountedUnitPrice()
    {
        var order = CreateTableOrder();
        SetupOrder(order);
        var product = CreateProduct(salePrice: 40m);
        SetupProduct(product);
        var promotion = Promotion.Create(
            branchId: order.BranchId, productId: product.Id, name: "Happy Hour",
            dayOfWeek: (int)DateTime.Now.DayOfWeek, startMinuteOfDay: 0, endMinuteOfDay: 1440,
            promotionTypeId: PromotionTypeIds.Desconto, discountRate: 0.25m).Value;
        _promotionRepository.GetByBranchAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion> { promotion });
        var command = new AddOrderItemCommand(CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().ContainSingle();
        order.Items.First().UnitPrice.Should().Be(30m); // 40 * (1 - 0.25)
        order.Items.First().Notes.Should().Contain("Happy Hour");
    }

    [Fact]
    public async Task Handle_ActivePromotionEmDobro_ShouldAddBonusItemAndDeductStockForBothLines()
    {
        var order = CreateTableOrder();
        SetupOrder(order);
        var product = CreateProduct(salePrice: 15m);
        SetupProduct(product);
        var stock = new ProductStock(product.Id, initialBalance: 10m, minimumQuantity: 0m);
        _stockRepository.GetByProductIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(stock);
        var promotion = Promotion.Create(
            branchId: order.BranchId, productId: product.Id, name: "Em Dobro",
            dayOfWeek: (int)DateTime.Now.DayOfWeek, startMinuteOfDay: 0, endMinuteOfDay: 1440,
            promotionTypeId: PromotionTypeIds.EmDobro).Value;
        _promotionRepository.GetByBranchAsync(order.BranchId, Arg.Any<CancellationToken>())
            .Returns(new List<Promotion> { promotion });
        var command = new AddOrderItemCommand(CustomerOrderId: 1, ProductId: product.Id, Quantity: 2, Notes: null, EmployeeId: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(2);
        order.Items.ElementAt(0).UnitPrice.Should().Be(15m);
        order.Items.ElementAt(1).UnitPrice.Should().Be(0m);
        // A baixa de estoque soma TODAS as linhas lançadas nesta chamada (linha principal + bônus).
        stock.CurrentBalance.Should().Be(6m); // 10 - (2 + 2)
    }

    [Fact]
    public async Task Handle_ComplementsSharingLinkedProduct_ShouldShareStockSnapshotAndFailOnCumulativeInsufficiency()
    {
        var order = CreateTableOrder();
        SetupOrder(order);
        var product = CreateProduct();
        SetupProduct(product);
        // Stock do produto principal não configurado (null) — isola o teste na baixa vinculada.
        var link = ProductComplementGroup.Create(product.Id, complementGroupId: 5, displayOrder: 0).Value;
        _productComplementGroupRepository.GetByProductAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        var group = ComplementGroup.Create(1, "Combo", ComplementGroupTypeIds.Especificacao, 0, 5).Value;
        var complementA = group.AddComplement(complementItemId: 101, extraPrice: 0m).Value;
        var complementB = group.AddComplement(complementItemId: 102, extraPrice: 0m).Value;
        _complementGroupRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(group);

        const long linkedProductId = 999;
        var complementItemA = CreateComplementItem(101, linkedProductId);
        var complementItemB = CreateComplementItem(102, linkedProductId);
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ComplementItem> { complementItemA, complementItemB });

        // Só há saldo pra UMA baixa de 1 unidade — se cada complemento consultasse um snapshot
        // próprio (sem compartilhar a instância), as duas baixas passariam incorretamente.
        var linkedStock = new ProductStock(linkedProductId, initialBalance: 1m, minimumQuantity: 0m);
        _stockRepository.GetByProductIdAsync(linkedProductId, Arg.Any<CancellationToken>()).Returns(linkedStock);

        var command = new AddOrderItemCommand(
            CustomerOrderId: 1, ProductId: product.Id, Quantity: 1, Notes: null, EmployeeId: null,
            Complements:
            [
                new OrderItemComplementSelection(ComplementGroupId: 5, ComplementId: complementA.Id),
                new OrderItemComplementSelection(ComplementGroupId: 5, ComplementId: complementB.Id)
            ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Stock.Insufficient");
        // Uma única consulta ao repositório de estoque, reaproveitada entre os dois complementos.
        await _stockRepository.Received(1).GetByProductIdAsync(linkedProductId, Arg.Any<CancellationToken>());
    }
}
