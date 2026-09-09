using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.Storefront.AddOrder;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Storefront.AddOrder;

public sealed class AddWebStorefrontOrderCommandHandlerTests
{
    private const long BranchId = 1;
    private const long CompanyId = 1;
    private const long SelfServiceEmployeeId = 99;
    private const long ProductId = 55;

    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly ICustomerAddressRepository _customerAddressRepository = Substitute.For<ICustomerAddressRepository>();
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AddWebStorefrontOrderCommandHandler _handler;

    public AddWebStorefrontOrderCommandHandlerTests()
    {
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero));

        _handler = new AddWebStorefrontOrderCommandHandler(
            _branchRepository, _productRepository, _orderRepository, _customerAddressRepository,
            _productComplementGroupRepository, _complementGroupRepository, _printingService,
            _timeProvider, _logRepository, _unitOfWork);
    }

    private static Branch MakeBranch(bool selfServiceEnabled = true, long companyId = CompanyId)
    {
        var branch = Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;
        if (selfServiceEnabled)
            branch.SetSelfServiceEmployee(SelfServiceEmployeeId);
        return branch;
    }

    private static Product MakeProduct(decimal salePrice = 20m, long companyId = CompanyId)
        => Product.Create(companyId, 1, 1, "X-Burger", null, null, salePrice, null, false, null).Value;

    private static CustomerAddress MakeAddress(long? customerId, string street = "Rua A", string number = "10", string supplement = "", string zipCode = "00000-000")
        => CustomerAddress.Create(CompanyId, BranchId, customerId, street, number, supplement, zipCode).Value;

    private static (ComplementGroup Group, Complement Complement) MakeComplementGroupWithComplement(long complementId, decimal extraPrice)
    {
        var group = ComplementGroup.Create(CompanyId, "Adicionais", 1, 0, 3).Value;
        var complement = group.AddComplement(1, extraPrice).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(complement, complementId);
        return (group, complement);
    }

    private void SetupValidBranchAndProduct(Branch branch, Product? product)
    {
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _productRepository.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
    }

    private static AddWebStorefrontOrderCommand MakeCommand(
        long? customerId = null,
        string customerName = "João",
        IReadOnlyCollection<WebStorefrontItemDto>? items = null)
        => new(
            BranchId,
            customerId,
            customerName,
            "11988887777",
            "Sem cebola por favor",
            items ?? new List<WebStorefrontItemDto> { new(ProductId, 1, null, null) });

    // ---------- Falhas ----------

    [Theory]
    [InlineData("valid")]
    [InlineData("duplicate")]
    [InlineData("foreign-product")]
    [InlineData("inactive")]
    [InlineData("disabled")]
    public async Task Handle_Customizations_ValidateOwnershipAndChargeStoredPricePerUnit(string scenario)
    {
        var product = MakeProduct();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(product, ProductId);
        product.ToggleExtrasAndBoosts(true, scenario != "disabled");
        var optional = ProductOptionalExtra.Create(ProductId, "Sem gelo", 0).Value;
        var boost = ProductBoost.Create(scenario == "foreign-product" ? 999 : ProductId, "Limão extra", 2.50m, 0).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(optional, 71L);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(boost, 72L);
        if (scenario == "inactive") boost.Deactivate();
        product.OptionalExtras.Add(optional);
        product.Boosts.Add(boost);
        SetupValidBranchAndProduct(MakeBranch(), product);
        var command = MakeCommand(items: [new WebStorefrontItemDto(ProductId, 3, null, null, [71], scenario == "duplicate" ? [72, 72] : [72])]);
        CustomerOrder? saved = null;
        await _orderRepository.AddAsync(Arg.Do<CustomerOrder>(order => saved = order), Arg.Any<CancellationToken>());

        var result = await _handler.Handle(command, default);

        if (scenario == "valid")
        {
            result.IsSuccess.Should().BeTrue();
            saved!.TotalAmount.Should().Be(67.50m);
            saved.Items.Single().UnitPrice.Should().Be(22.50m);
            saved.Items.Single().OptionalExtras.Single().Name.Should().Be("Sem gelo");
            saved.Items.Single().Boosts.Single().UnitPriceCharged.Should().Be(2.50m);
        }
        else
        {
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(scenario == "duplicate" ? "OrderItem.DuplicateSelection" : "OrderItem.BoostUnavailable");
            saved.Should().BeNull();
        }
    }

    [Fact]
    public async Task Handle_EmptyCart_ShouldReturnCartEmptyFailure()
    {
        var command = MakeCommand(items: Array.Empty<WebStorefrontItemDto>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Cart.Empty");
        await _branchRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldReturnFailure()
    {
        var command = MakeCommand();
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BranchInactive_ShouldReturnFailure()
    {
        var branch = MakeBranch();
        branch.Deactivate();
        var command = MakeCommand();
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(branch);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_SelfServiceDisabled_ShouldReturnFailure()
    {
        var branch = MakeBranch(selfServiceEnabled: false);
        var command = MakeCommand();
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(branch);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.SelfServiceDisabled");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        var branch = MakeBranch();
        var command = MakeCommand();
        SetupValidBranchAndProduct(branch, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ProductInactive_ShouldReturnFailure()
    {
        var branch = MakeBranch();
        var product = MakeProduct();
        product.Deactivate();
        var command = MakeCommand();
        SetupValidBranchAndProduct(branch, product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ProductFromDifferentCompany_ShouldReturnFailure()
    {
        var branch = MakeBranch();
        var product = MakeProduct(companyId: 2);
        var command = MakeCommand();
        SetupValidBranchAndProduct(branch, product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_EmptyCustomerName_ShouldReturnFailureFromCustomerOrderCreate()
    {
        var branch = MakeBranch();
        var product = MakeProduct();
        var command = MakeCommand(customerName: " ");
        SetupValidBranchAndProduct(branch, product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.MissingCustomerName");
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidItemQuantity_ShouldReturnFailure()
    {
        var branch = MakeBranch();
        var product = MakeProduct();
        var command = MakeCommand(items: new List<WebStorefrontItemDto> { new(ProductId, 0, null, null) });
        SetupValidBranchAndProduct(branch, product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.InvalidQuantity");
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComplementGroupNotInAllowedList_ShouldReturnFailure()
    {
        var branch = MakeBranch();
        var product = MakeProduct();
        var command = MakeCommand(items: new List<WebStorefrontItemDto>
        {
            new(ProductId, 1, null, new List<OrderItemComplementSelection> { new(10, 100) })
        });
        SetupValidBranchAndProduct(branch, product);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.ComplementGroupNotAvailable");
    }

    [Fact]
    public async Task Handle_ComplementGroupNotFound_ShouldReturnFailure()
    {
        var branch = MakeBranch();
        var product = MakeProduct();
        var link = ProductComplementGroup.Create(ProductId, 10, 0).Value;
        var command = MakeCommand(items: new List<WebStorefrontItemDto>
        {
            new(ProductId, 1, null, new List<OrderItemComplementSelection> { new(10, 100) })
        });
        SetupValidBranchAndProduct(branch, product);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((ComplementGroup?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_ComplementGroupInactive_ShouldReturnFailure()
    {
        var branch = MakeBranch();
        var product = MakeProduct();
        var link = ProductComplementGroup.Create(ProductId, 10, 0).Value;
        var (group, _) = MakeComplementGroupWithComplement(100, 5m);
        group.Deactivate();
        var command = MakeCommand(items: new List<WebStorefrontItemDto>
        {
            new(ProductId, 1, null, new List<OrderItemComplementSelection> { new(10, 100) })
        });
        SetupValidBranchAndProduct(branch, product);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_ComplementNotFoundInGroup_ShouldReturnFailure()
    {
        var branch = MakeBranch();
        var product = MakeProduct();
        var link = ProductComplementGroup.Create(ProductId, 10, 0).Value;
        var (group, _) = MakeComplementGroupWithComplement(100, 5m);
        var command = MakeCommand(items: new List<WebStorefrontItemDto>
        {
            new(ProductId, 1, null, new List<OrderItemComplementSelection> { new(10, 999) })
        });
        SetupValidBranchAndProduct(branch, product);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.ComplementNotFound");
    }

    [Fact]
    public async Task Handle_ComplementInactiveInGroup_ShouldReturnFailure()
    {
        var branch = MakeBranch();
        var product = MakeProduct();
        var link = ProductComplementGroup.Create(ProductId, 10, 0).Value;
        var (group, complement) = MakeComplementGroupWithComplement(100, 5m);
        group.RemoveComplement(complement.Id);
        var command = MakeCommand(items: new List<WebStorefrontItemDto>
        {
            new(ProductId, 1, null, new List<OrderItemComplementSelection> { new(10, 100) })
        });
        SetupValidBranchAndProduct(branch, product);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.ComplementNotFound");
    }

    // ---------- Caminho feliz ----------

    [Fact]
    public async Task Handle_NoCustomerLinked_ShouldCreateOrderWithoutQueryingAddressesAndPrint()
    {
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 20m);
        CustomerOrder? capturedOrder = null;
        var command = MakeCommand(customerId: null);
        SetupValidBranchAndProduct(branch, product);
        _orderRepository.When(x => x.AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>()))
            .Do(ci => capturedOrder = ci.Arg<CustomerOrder>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.EmployeeId.Should().Be(SelfServiceEmployeeId);
        capturedOrder.CustomerId.Should().BeNull();
        capturedOrder.Items.Should().ContainSingle();
        capturedOrder.OrderOriginId.Should().Be(OrderOriginIds.WebSite);
        await _customerAddressRepository.DidNotReceiveWithAnyArgs().GetByCustomerIdAsync(default, default);
        await _customerAddressRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await _printingService.Received(1).PrintOrderItemsAsync(
            Arg.Any<long>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
        // Um commit explícito ao salvar o pedido + um commit do log da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerWithActiveAddress_ShouldUpdateAddressWithLastOrderId()
    {
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 20m);
        var address = MakeAddress(customerId: 7, supplement: "Apto 2");
        var command = MakeCommand(customerId: 7);
        SetupValidBranchAndProduct(branch, product);
        _customerAddressRepository.GetByCustomerIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<CustomerAddress> { address });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        address.LastOrderId.Should().NotBeNull();
        await _customerAddressRepository.Received(1).UpdateAsync(
            Arg.Is<CustomerAddress>(a => a == address), Arg.Any<CancellationToken>());
        // Commit ao salvar o pedido + commit ao atualizar o endereço + commit do log da base.
        await _unitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerWithoutActiveAddress_ShouldNotUpdateAnyAddress()
    {
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 20m);
        var address = MakeAddress(customerId: 7);
        address.Deactivate();
        var command = MakeCommand(customerId: 7);
        SetupValidBranchAndProduct(branch, product);
        _customerAddressRepository.GetByCustomerIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<CustomerAddress> { address });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _customerAddressRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomerIdZero_ShouldBeTreatedAsNoCustomerLinked()
    {
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 20m);
        var command = MakeCommand(customerId: 0);
        SetupValidBranchAndProduct(branch, product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _customerAddressRepository.DidNotReceiveWithAnyArgs().GetByCustomerIdAsync(default, default);
    }

    [Fact]
    public async Task Handle_ValidComplementSelection_ShouldAddComplementToItem()
    {
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 20m);
        var link = ProductComplementGroup.Create(ProductId, 10, 0).Value;
        var (group, _) = MakeComplementGroupWithComplement(100, 3.5m);
        CustomerOrder? capturedOrder = null;
        var command = MakeCommand(items: new List<WebStorefrontItemDto>
        {
            new(ProductId, 1, "Bem passado", new List<OrderItemComplementSelection> { new(10, 100) })
        });
        SetupValidBranchAndProduct(branch, product);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(group);
        _orderRepository.When(x => x.AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>()))
            .Do(ci => capturedOrder = ci.Arg<CustomerOrder>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        var item = capturedOrder!.Items.Should().ContainSingle().Subject;
        item.Notes.Should().Contain("Bem passado");
        item.Complements.Should().ContainSingle();
        item.Complements.First().ComplementId.Should().Be(100);
        item.Complements.First().UnitPriceCharged.Should().Be(3.5m);
    }

    [Fact]
    public async Task Handle_PrintingServiceThrows_ShouldStillReturnSuccess()
    {
        var branch = MakeBranch();
        var product = MakeProduct();
        var command = MakeCommand();
        SetupValidBranchAndProduct(branch, product);
        _printingService.PrintOrderItemsAsync(Arg.Any<long>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("printer offline"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
