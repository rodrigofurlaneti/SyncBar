using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Stock.AdjustInventory;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Stock.AdjustInventory;

public sealed class AdjustInventoryCommandHandlerTests
{
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AdjustInventoryCommandHandler _handler;

    public AdjustInventoryCommandHandlerTests()
    {
        _handler = new AdjustInventoryCommandHandler(_stockItemRepository, _stockMovementRepository, _logRepository, _unitOfWork);
    }

    private static StockItem CreateStockItemWithBalance(long branchId, long productId, decimal currentQuantity)
    {
        var item = StockItem.Create(branchId, productId, 0, null).Value;
        if (currentQuantity > 0)
            item.Increase(currentQuantity);
        return item;
    }

    [Fact]
    public async Task Handle_CountEqualsCurrentQuantity_ShouldSkipAdjustmentAndNotCreateMovement()
    {
        var command = new AdjustInventoryCommand(BranchId: 1, EmployeeId: 1, Counts: [new InventoryCountInput(1, 10)]);
        var stockItem = CreateStockItemWithBalance(1, 1, 10);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _stockMovementRepository.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        // Commit explícito do handler (sempre roda após o laço) + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CountHigherThanCurrent_ShouldIncreaseAndRegisterAjusteEntradaMovement()
    {
        var command = new AdjustInventoryCommand(BranchId: 1, EmployeeId: 7, Counts: [new InventoryCountInput(1, 15)]);
        var stockItem = CreateStockItemWithBalance(1, 1, 10);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var adjustment = result.Value.Single();
        adjustment.ProductId.Should().Be(1);
        adjustment.PreviousQuantity.Should().Be(10);
        adjustment.CountedQuantity.Should().Be(15);
        adjustment.Difference.Should().Be(5);
        stockItem.CurrentQuantity.Should().Be(15);

        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.StockMovementTypeId == StockMovementTypeIds.AjusteEntrada && m.Quantity == 5 && m.EmployeeId == 7),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CountLowerThanCurrent_ShouldDecreaseAndRegisterAjusteSaidaMovement()
    {
        var command = new AdjustInventoryCommand(BranchId: 1, EmployeeId: 7, Counts: [new InventoryCountInput(1, 4)]);
        var stockItem = CreateStockItemWithBalance(1, 1, 10);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var adjustment = result.Value.Single();
        adjustment.Difference.Should().Be(-6);
        stockItem.CurrentQuantity.Should().Be(4);

        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.StockMovementTypeId == StockMovementTypeIds.AjusteSaida && m.Quantity == 6),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductWithoutStockItem_ShouldCreateStockItemWithZeroBalanceAndCommitExtra()
    {
        var command = new AdjustInventoryCommand(BranchId: 1, EmployeeId: 7, Counts: [new InventoryCountInput(9, 8)]);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 9, Arg.Any<CancellationToken>()).Returns((StockItem?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var adjustment = result.Value.Single();
        adjustment.PreviousQuantity.Should().Be(0);
        adjustment.Difference.Should().Be(8);

        await _stockItemRepository.Received(1).AddAsync(Arg.Any<StockItem>(), Arg.Any<CancellationToken>());
        // Commit da criação do StockItem + commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DecreaseWouldGoNegative_ShouldReturnFailureWithoutRegisteringMovement()
    {
        var command = new AdjustInventoryCommand(BranchId: 1, EmployeeId: 7, Counts: [new InventoryCountInput(1, -1)]);
        var stockItem = CreateStockItemWithBalance(1, 1, 5);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StockItem.InsufficientStock");
        await _stockMovementRepository.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        // Falha interrompe o laço antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleCounts_ShouldProcessEachAndReturnAllAdjustmentsInOrder()
    {
        var command = new AdjustInventoryCommand(
            BranchId: 1, EmployeeId: 7,
            Counts: [new InventoryCountInput(1, 12), new InventoryCountInput(2, 3)]);
        var itemOne = CreateStockItemWithBalance(1, 1, 10);
        var itemTwo = CreateStockItemWithBalance(1, 2, 10);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns(itemOne);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 2, Arg.Any<CancellationToken>()).Returns(itemTwo);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(a => a.ProductId).Should().ContainInOrder(1, 2);
        await _stockMovementRepository.Received(2).AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
