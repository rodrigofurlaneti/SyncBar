using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.PublicOrdering.AddItem;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.PublicOrdering.AddItem;

public sealed class AddPublicOrderItemCommandHandlerTests
{
    private const long BranchId = 1;
    private const long CompanyId = 1;
    private const long SelfServiceEmployeeId = 99;
    private const long ProductId = 55;
    private const string ComandaCode = "001";

    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IComandaRepository _comandaRepository = Substitute.For<IComandaRepository>();
    private readonly IComandaSettingRepository _comandaSettingRepository = Substitute.For<IComandaSettingRepository>();
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AddPublicOrderItemCommandHandler _handler;

    public AddPublicOrderItemCommandHandlerTests()
    {
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero));

        _handler = new AddPublicOrderItemCommandHandler(
            _diningTableRepository, _branchRepository, _productRepository, _orderRepository,
            _comandaRepository, _comandaSettingRepository, _productComplementGroupRepository,
            _complementGroupRepository, _printingService, _timeProvider, _logRepository, _unitOfWork);

        // Sem pedido aberto por padrão — cada teste decide se precisa simular um já existente.
        _orderRepository.GetOpenByTableForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns((CustomerOrder?)null);
        _orderRepository.GetOpenByComandaForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns((CustomerOrder?)null);
    }

    private static DiningTable MakeTable(int number = 7) => DiningTable.Create(BranchId, TableStatusIds.Livre, number, 4).Value;

    private static Branch MakeBranch(bool selfServiceEnabled = true)
    {
        var branch = Branch.Create(CompanyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;
        if (selfServiceEnabled)
            branch.SetSelfServiceEmployee(SelfServiceEmployeeId);
        return branch;
    }

    private static Product MakeProduct(decimal salePrice = 20m, long companyId = CompanyId)
        => Product.Create(companyId, 1, 1, "X-Burger", null, null, salePrice, null, false, null).Value;

    private static Comanda MakeComanda(string code = ComandaCode) => Comanda.Create(BranchId, ComandaStatusIds.Disponivel, code).Value;

    private static (ComplementGroup Group, Complement Complement) MakeComplementGroupWithComplement(long complementId, decimal extraPrice)
    {
        var group = ComplementGroup.Create(CompanyId, "Adicionais", ComplementGroupTypeIds.SelecaoAdicional, 0, 3).Value;
        var complement = group.AddComplement(1, extraPrice).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(complement, complementId);
        return (group, complement);
    }

    private void SetupValidTableBranchAndProduct(Guid token, DiningTable table, Branch branch, Product? product)
    {
        _diningTableRepository.GetByQrTokenAsync(token, Arg.Any<CancellationToken>()).Returns(table);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(branch);
        _productRepository.GetByIdAsync(ProductId, Arg.Any<CancellationToken>()).Returns(product);
    }

    // ---------- Falhas ----------

    [Fact]
    public async Task Handle_InvalidToken_ShouldReturnFailure()
    {
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        _diningTableRepository.GetByQrTokenAsync(command.Token, Arg.Any<CancellationToken>()).Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.InvalidToken");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveTable_ShouldReturnFailure()
    {
        var table = MakeTable();
        table.Deactivate();
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        _diningTableRepository.GetByQrTokenAsync(command.Token, Arg.Any<CancellationToken>()).Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.InvalidToken");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldReturnFailure()
    {
        var table = MakeTable();
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        _diningTableRepository.GetByQrTokenAsync(command.Token, Arg.Any<CancellationToken>()).Returns(table);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveBranch_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        branch.Deactivate();
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        _diningTableRepository.GetByQrTokenAsync(command.Token, Arg.Any<CancellationToken>()).Returns(table);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(branch);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SelfServiceDisabled_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch(selfServiceEnabled: false);
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        _diningTableRepository.GetByQrTokenAsync(command.Token, Arg.Any<CancellationToken>()).Returns(table);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(branch);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.SelfServiceDisabled");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        SetupValidTableBranchAndProduct(command.Token, table, branch, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveProduct_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct();
        product.Deactivate();
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductFromDifferentCompany_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct(companyId: 2);
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComandaCodeNotFound_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct();
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null, ComandaCode: "999");
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _comandaRepository.GetByCodeAsync(BranchId, "999", Arg.Any<CancellationToken>()).Returns((Comanda?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveComanda_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct();
        var comanda = MakeComanda();
        comanda.Deactivate();
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null, ComandaCode: ComandaCode);
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _comandaRepository.GetByCodeAsync(BranchId, ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComplementGroupNotAvailableForProduct_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct();
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null,
            Complements: new List<OrderItemComplementSelection> { new(10, 100) });
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderItem.ComplementGroupNotAvailable");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComplementGroupNotFound_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct();
        var link = ProductComplementGroup.Create(ProductId, 10, 0).Value;
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null,
            Complements: new List<OrderItemComplementSelection> { new(10, 100) });
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((ComplementGroup?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComplementGroupInactive_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct();
        var link = ProductComplementGroup.Create(ProductId, 10, 0).Value;
        var (group, _) = MakeComplementGroupWithComplement(100, 5m);
        group.Deactivate();
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null,
            Complements: new List<OrderItemComplementSelection> { new(10, 100) });
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComplementNotFoundInGroup_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct();
        var link = ProductComplementGroup.Create(ProductId, 10, 0).Value;
        var (group, _) = MakeComplementGroupWithComplement(100, 5m);
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null,
            Complements: new List<OrderItemComplementSelection> { new(10, 999) });
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.ComplementNotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ZeroQuantityOnExistingOrder_ShouldReturnFailureWithSingleCommit()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct();
        var existingOrder = CustomerOrder.Create(BranchId, table.Id, null, SelfServiceEmployeeId, null, "Pedido via QR Code", DateTime.Now).Value;
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 0, null);
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _orderRepository.GetOpenByTableForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(existingOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.InvalidQuantity");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _orderRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ComandaCreditLimitExceeded_ShouldReturnFailureAfterCreatingComandaOrder()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 50m);
        var comanda = MakeComanda();
        var comandaSetting = ComandaSetting.Create(BranchId, 5m).Value;
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null, ComandaCode: ComandaCode);
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _comandaRepository.GetByCodeAsync(BranchId, ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);
        _comandaSettingRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(comandaSetting);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.LimitExceeded");
        await _orderRepository.Received(1).AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---------- Caminho feliz ----------

    [Fact]
    public async Task Handle_NewTableOrder_ShouldCreateOrderMarkTableOccupiedAddItemAndPrint()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 20m);
        CustomerOrder? capturedOrder = null;
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 2, "Sem cebola");
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _orderRepository.When(x => x.AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>()))
            .Do(ci => capturedOrder = ci.Arg<CustomerOrder>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.DiningTableId.Should().Be(table.Id);
        capturedOrder.ComandaId.Should().BeNull();
        capturedOrder.OrderTypeId.Should().Be(OrderTypeIds.Mesa);
        capturedOrder.EmployeeId.Should().Be(SelfServiceEmployeeId);
        capturedOrder.Items.Should().ContainSingle();
        capturedOrder.Items.First().Quantity.Should().Be(2);
        capturedOrder.Items.First().UnitPrice.Should().Be(20m);
        capturedOrder.Items.First().Notes.Should().Be("Sem cebola");
        table.TableStatusId.Should().Be(TableStatusIds.Ocupada);
        _diningTableRepository.Received(1).Update(Arg.Is<DiningTable>(t => t == table));
        await _unitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
        await _printingService.Received(1).PrintOrderItemsAsync(
            Arg.Any<long>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingTableOrder_ShouldAddItemWithoutRecreatingOrderOrTouchingTable()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 15m);
        var existingOrder = CustomerOrder.Create(BranchId, table.Id, null, SelfServiceEmployeeId, null, "Pedido via QR Code", DateTime.Now).Value;
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _orderRepository.GetOpenByTableForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(existingOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingOrder.Items.Should().ContainSingle();
        await _orderRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        _diningTableRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewComandaOrder_ShouldLinkOrderToComandaWithCreditLimitAndOccupyTable()
    {
        var table = MakeTable(number: 7);
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 20m);
        var comanda = MakeComanda();
        var comandaSetting = ComandaSetting.Create(BranchId, 1000m).Value;
        CustomerOrder? capturedOrder = null;
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null, ComandaCode: ComandaCode);
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _comandaRepository.GetByCodeAsync(BranchId, ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);
        _comandaSettingRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(comandaSetting);
        _orderRepository.When(x => x.AddAsync(Arg.Any<CustomerOrder>(), Arg.Any<CancellationToken>()))
            .Do(ci => capturedOrder = ci.Arg<CustomerOrder>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.DiningTableId.Should().BeNull();
        capturedOrder.ComandaId.Should().Be(comanda.Id);
        capturedOrder.CreditLimitAmount.Should().Be(1000m);
        capturedOrder.Notes.Should().Contain("Mesa 7");
        table.TableStatusId.Should().Be(TableStatusIds.Ocupada);
        _diningTableRepository.Received(1).Update(Arg.Any<DiningTable>());
        await _unitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingComandaOrder_ShouldReuseOrderAndSkipComandaSettingLookup()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 10m);
        var comanda = MakeComanda();
        var existingOrder = CustomerOrder.Create(
            BranchId, null, comanda.Id, SelfServiceEmployeeId, null, "Mesa 7 — Pedido via QR Code", DateTime.Now, 500m).Value;
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null, ComandaCode: ComandaCode);
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _comandaRepository.GetByCodeAsync(BranchId, ComandaCode, Arg.Any<CancellationToken>()).Returns(comanda);
        _orderRepository.GetOpenByComandaForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(existingOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingOrder.Items.Should().ContainSingle();
        await _comandaSettingRepository.DidNotReceiveWithAnyArgs().GetByBranchAsync(default, default);
        _diningTableRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidComplementSelection_ShouldAddComplementToItem()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct(salePrice: 20m);
        var existingOrder = CustomerOrder.Create(BranchId, table.Id, null, SelfServiceEmployeeId, null, "Pedido via QR Code", DateTime.Now).Value;
        var link = ProductComplementGroup.Create(ProductId, 10, 0).Value;
        var (group, _) = MakeComplementGroupWithComplement(100, 3.5m);
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null,
            Complements: new List<OrderItemComplementSelection> { new(10, 100) });
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _orderRepository.GetOpenByTableForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(existingOrder);
        _productComplementGroupRepository.GetByProductAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = existingOrder.Items.Should().ContainSingle().Subject;
        item.Complements.Should().ContainSingle();
        item.Complements.First().ComplementId.Should().Be(100);
        item.Complements.First().UnitPriceCharged.Should().Be(3.5m);
        item.TotalAmount.Should().Be(20m + 3.5m);
    }

    [Fact]
    public async Task Handle_NoComandaCode_ShouldNotQueryComandaRepository()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct();
        var existingOrder = CustomerOrder.Create(BranchId, table.Id, null, SelfServiceEmployeeId, null, "Pedido via QR Code", DateTime.Now).Value;
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _orderRepository.GetOpenByTableForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(existingOrder);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _comandaRepository.DidNotReceiveWithAnyArgs().GetByCodeAsync(default, default!, default);
    }

    [Fact]
    public async Task Handle_PrintingServiceThrows_ShouldStillReturnSuccess()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct();
        var existingOrder = CustomerOrder.Create(BranchId, table.Id, null, SelfServiceEmployeeId, null, "Pedido via QR Code", DateTime.Now).Value;
        var command = new AddPublicOrderItemCommand(Guid.NewGuid(), ProductId, 1, null);
        SetupValidTableBranchAndProduct(command.Token, table, branch, product);
        _orderRepository.GetOpenByTableForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(existingOrder);
        _printingService.PrintOrderItemsAsync(Arg.Any<long>(), Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("printer offline"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
