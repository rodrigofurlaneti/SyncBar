using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Stock.GetLedger;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Stock.GetLedger;

public sealed class GetStockLedgerQueryHandlerTests
{
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetStockLedgerQueryHandler _handler;

    public GetStockLedgerQueryHandlerTests()
    {
        _handler = new GetStockLedgerQueryHandler(_stockMovementRepository, _logRepository, _unitOfWork);
    }

    private static StockMovement CreateMovement(long stockItemId, DateTime movedAt, decimal quantity = 1, decimal? unitCost = null, decimal? totalCost = null, string? documentNumber = null, string? notes = null)
        => StockMovement.Create(
            stockItemId, StockMovementTypeIds.EntradaCompra, null, null, null,
            quantity, unitCost, totalCost, documentNumber, movedAt, notes).Value;

    [Fact]
    public async Task Handle_NoMovementsForStockItem_ShouldReturnEmptyCollection()
    {
        var query = new GetStockLedgerQuery(StockItemId: 1);
        _stockMovementRepository.GetByStockItemAsync(query.StockItemId, Arg.Any<CancellationToken>()).Returns(Array.Empty<StockMovement>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleMovements_ShouldOrderByMovedAtDescendingAndMapFields()
    {
        var query = new GetStockLedgerQuery(StockItemId: 1);
        var older = CreateMovement(1, DateTime.Now.AddDays(-2), quantity: 10, unitCost: 2m, totalCost: 20m, documentNumber: "NF-1", notes: "Compra");
        var newer = CreateMovement(1, DateTime.Now.AddDays(-1), quantity: 4, notes: "Venda");
        _stockMovementRepository.GetByStockItemAsync(query.StockItemId, Arg.Any<CancellationToken>()).Returns([older, newer]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.First().Notes.Should().Be("Venda"); // mais recente primeiro

        var responseOlder = result.Value.Last();
        responseOlder.StockItemId.Should().Be(1);
        responseOlder.Quantity.Should().Be(10);
        responseOlder.UnitCost.Should().Be(2m);
        responseOlder.TotalCost.Should().Be(20m);
        responseOlder.DocumentNumber.Should().Be("NF-1");
    }
}
