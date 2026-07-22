using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Stock.AdjustInventory;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class AdjustInventoryCommandHandlerTests
{
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private AdjustInventoryCommandHandler CreateHandler()
        => new(_stockItemRepository, _stockMovementRepository, _unitOfWork);

    [Fact]
    public async Task Handle_ShouldAdjustUpAndDownAndSkipMatches()
    {
        var beer = StockItem.Create(1, 1, 10, null).Value;   // saldo 20, contado 24 → +4
        beer.Increase(20);
        var soda = StockItem.Create(1, 2, 10, null).Value;   // saldo 15, contado 12 → −3
        soda.Increase(15);
        var water = StockItem.Create(1, 3, 10, null).Value;  // saldo 8, contado 8 → nada
        water.Increase(8);

        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 1, Arg.Any<CancellationToken>()).Returns(beer);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 2, Arg.Any<CancellationToken>()).Returns(soda);
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 3, Arg.Any<CancellationToken>()).Returns(water);

        var result = await CreateHandler().Handle(new AdjustInventoryCommand(1, 1,
        [
            new InventoryCountInput(1, 24),
            new InventoryCountInput(2, 12),
            new InventoryCountInput(3, 8),
        ]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2); // agua bateu, nao gera ajuste
        beer.CurrentQuantity.Should().Be(24);
        soda.CurrentQuantity.Should().Be(12);
        water.CurrentQuantity.Should().Be(8);

        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.StockMovementTypeId == StockMovementTypeIds.AjusteEntrada && m.Quantity == 4),
            Arg.Any<CancellationToken>());
        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.StockMovementTypeId == StockMovementTypeIds.AjusteSaida && m.Quantity == 3),
            Arg.Any<CancellationToken>());
    }
}
