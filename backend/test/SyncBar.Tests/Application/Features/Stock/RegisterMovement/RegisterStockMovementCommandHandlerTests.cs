using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Application.Features.Stock.RegisterMovement;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Stock.RegisterMovement;

public sealed class RegisterStockMovementCommandHandlerTests
{
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RegisterStockMovementCommandHandler _handler;

    public RegisterStockMovementCommandHandlerTests()
    {
        _handler = new RegisterStockMovementCommandHandler(
            _stockItemRepository, _stockMovementRepository, _productRepository, _employeeRepository, _currentUser, _logRepository, _unitOfWork);
    }

    private static RegisterStockMovementCommand CreateCommand(
        long stockMovementTypeId, decimal quantity = 5, decimal? unitCost = null, long productId = 1, long branchId = 1)
        => new(branchId, productId, stockMovementTypeId, EmployeeId: 0, quantity, unitCost, DocumentNumber: null, Notes: null);

    private static Employee CreateActiveEmployee()
        => Employee.Create(branchId: 1, jobTitleId: 1, name: "Func. Teste", cpf: "12345678901", email: null, phone: null, hiredAt: DateTime.Now, dismissedAt: null, salary: null).Value;

    private static Product CreateActiveProduct()
        => Product.Create(companyId: 1, categoryId: 1, unitOfMeasureId: 1, name: "Produto Teste", description: null, barcode: null, salePrice: 10m, costPrice: null, isStockControlled: true, preparationTimeMinutes: null).Value;

    private static StockItem CreateStockItemWithBalance(long branchId, long productId, decimal currentQuantity)
    {
        var item = StockItem.Create(branchId, productId, 0, null).Value;
        if (currentQuantity > 0)
            item.Increase(currentQuantity);
        return item;
    }

    private void SetupValidEmployeeAndProduct(Employee employee, Product product)
    {
        _currentUser.EmployeeId.Returns((long?)employee.Id);
        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        _productRepository.GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(product);
    }

    [Fact]
    public async Task Handle_CurrentUserWithoutEmployeeLinked_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(StockMovementTypeIds.EntradaCompra);
        _currentUser.EmployeeId.Returns((long?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
        await _stockMovementRepository.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        // Nenhum commit explícito do handler; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmployeeNotFoundOrInactive_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(StockMovementTypeIds.EntradaCompra);
        _currentUser.EmployeeId.Returns((long?)10);
        _employeeRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductNotFoundOrInactive_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(StockMovementTypeIds.EntradaCompra);
        var employee = CreateActiveEmployee();
        _currentUser.EmployeeId.Returns((long?)employee.Id);
        _employeeRepository.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        _productRepository.GetByIdAsync(command.ProductId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InflowMovementWithExistingStockItem_ShouldIncreaseStockAndPersistMovementWithComputedTotalCost()
    {
        var command = CreateCommand(StockMovementTypeIds.EntradaCompra, quantity: 5, unitCost: 2.5m);
        var employee = CreateActiveEmployee();
        var product = CreateActiveProduct();
        SetupValidEmployeeAndProduct(employee, product);
        var stockItem = CreateStockItemWithBalance(command.BranchId, command.ProductId, 10);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(command.BranchId, command.ProductId, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stockItem.CurrentQuantity.Should().Be(15);
        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.UnitCost == 2.5m && m.TotalCost == 12.5m && m.StockMovementTypeId == StockMovementTypeIds.EntradaCompra),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler + commit do finally da base (item já existia).
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OutflowMovementWithExistingStockItem_ShouldDecreaseStock()
    {
        var command = CreateCommand(StockMovementTypeIds.SaidaVenda, quantity: 4);
        var employee = CreateActiveEmployee();
        var product = CreateActiveProduct();
        SetupValidEmployeeAndProduct(employee, product);
        var stockItem = CreateStockItemWithBalance(command.BranchId, command.ProductId, 10);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(command.BranchId, command.ProductId, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stockItem.CurrentQuantity.Should().Be(6);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OutflowExceedingCurrentStock_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateCommand(StockMovementTypeIds.SaidaVenda, quantity: 5);
        var employee = CreateActiveEmployee();
        var product = CreateActiveProduct();
        SetupValidEmployeeAndProduct(employee, product);
        var stockItem = CreateStockItemWithBalance(command.BranchId, command.ProductId, 3);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(command.BranchId, command.ProductId, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StockItem.InsufficientStock");
        await _stockMovementRepository.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductWithoutExistingStockItem_ShouldCreateStockItemWithExtraCommit()
    {
        var command = CreateCommand(StockMovementTypeIds.EntradaCompra, quantity: 8);
        var employee = CreateActiveEmployee();
        var product = CreateActiveProduct();
        SetupValidEmployeeAndProduct(employee, product);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(command.BranchId, command.ProductId, Arg.Any<CancellationToken>()).Returns((StockItem?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _stockItemRepository.Received(1).AddAsync(Arg.Any<StockItem>(), Arg.Any<CancellationToken>());
        // Commit da criação do StockItem + commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnitCostNull_ShouldPersistMovementWithNullTotalCost()
    {
        var command = CreateCommand(StockMovementTypeIds.EntradaCompra, quantity: 5, unitCost: null);
        var employee = CreateActiveEmployee();
        var product = CreateActiveProduct();
        SetupValidEmployeeAndProduct(employee, product);
        var stockItem = CreateStockItemWithBalance(command.BranchId, command.ProductId, 10);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(command.BranchId, command.ProductId, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.UnitCost == null && m.TotalCost == null),
            Arg.Any<CancellationToken>());
    }
}
