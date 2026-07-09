using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Stock.RegisterMovement;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class RegisterStockMovementCommandHandlerTests
{
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegisterStockMovementCommandHandler CreateHandler()
        => new(_stockItemRepository, _stockMovementRepository, _productRepository, _unitOfWork);

    private static Product SomeProduct()
        => Product.Create(1, 1, 1, "Cerveja", null, null, 10m, 5m, true, null).Value;

    [Fact]
    public async Task Handle_InflowMovement_ShouldIncreaseBalanceAndWriteLedger()
    {
        var stockItem = StockItem.Create(1, 5, 10, null).Value;
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(SomeProduct());
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 5, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await CreateHandler().Handle(
            new RegisterStockMovementCommand(1, 5, StockMovementTypeIds.EntradaCompra, 1, 24, 6.50m, "NF-123", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stockItem.CurrentQuantity.Should().Be(24);
        await _stockMovementRepository.Received(1).AddAsync(
            Arg.Is<StockMovement>(m => m.Quantity == 24 && m.TotalCost == 156m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OutflowBeyondBalance_ShouldFail()
    {
        var stockItem = StockItem.Create(1, 5, 10, null).Value;
        stockItem.Increase(3);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(SomeProduct());
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 5, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await CreateHandler().Handle(
            new RegisterStockMovementCommand(1, 5, StockMovementTypeIds.Perda, 1, 10, null, null, "quebra"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StockItem.InsufficientStock");
        await _stockMovementRepository.DidNotReceive().AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FirstMovementForProduct_ShouldCreateStockItem()
    {
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(SomeProduct());
        _stockItemRepository.GetByBranchAndProductForUpdateAsync(1, 5, Arg.Any<CancellationToken>())
            .Returns((StockItem?)null);

        var result = await CreateHandler().Handle(
            new RegisterStockMovementCommand(1, 5, StockMovementTypeIds.AjusteEntrada, 1, 12, null, null, "inventário"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _stockItemRepository.Received(1).AddAsync(Arg.Any<StockItem>(), Arg.Any<CancellationToken>());
        await _stockMovementRepository.Received(1).AddAsync(Arg.Any<StockMovement>(), Arg.Any<CancellationToken>());
    }
}
