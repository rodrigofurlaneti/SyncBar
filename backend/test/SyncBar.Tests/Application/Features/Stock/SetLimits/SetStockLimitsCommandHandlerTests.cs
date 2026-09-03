using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Stock.SetLimits;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Stock.SetLimits;

public sealed class SetStockLimitsCommandHandlerTests
{
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetStockLimitsCommandHandler _handler;

    public SetStockLimitsCommandHandlerTests()
    {
        _handler = new SetStockLimitsCommandHandler(_stockItemRepository, _logRepository, _unitOfWork);
    }

    private static StockItem CreateActiveStockItem()
        => StockItem.Create(branchId: 1, productId: 1, minimumQuantity: 5, maximumQuantity: 50).Value;

    [Fact]
    public async Task Handle_StockItemNotFound_ShouldReturnFailureWithoutCommitting()
    {
        var command = new SetStockLimitsCommand(StockItemId: 1, MinimumQuantity: 10, MaximumQuantity: 100);
        _stockItemRepository.GetByIdForUpdateAsync(command.StockItemId, Arg.Any<CancellationToken>()).Returns((StockItem?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StockItem.NotFound");
        // Nenhum commit explícito do handler; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StockItemInactive_ShouldReturnFailureWithoutCommitting()
    {
        var stockItem = CreateActiveStockItem();
        stockItem.Deactivate();
        var command = new SetStockLimitsCommand(StockItemId: 1, MinimumQuantity: 10, MaximumQuantity: 100);
        _stockItemRepository.GetByIdForUpdateAsync(command.StockItemId, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StockItem.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MaximumBelowMinimum_ShouldReturnFailureWithoutCommitting()
    {
        var stockItem = CreateActiveStockItem();
        var command = new SetStockLimitsCommand(StockItemId: 1, MinimumQuantity: 10, MaximumQuantity: 5);
        _stockItemRepository.GetByIdForUpdateAsync(command.StockItemId, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StockItem.InvalidMaximum");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeMinimum_ShouldReturnFailureWithoutCommitting()
    {
        var stockItem = CreateActiveStockItem();
        var command = new SetStockLimitsCommand(StockItemId: 1, MinimumQuantity: -1, MaximumQuantity: null);
        _stockItemRepository.GetByIdForUpdateAsync(command.StockItemId, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StockItem.InvalidMinimum");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateLimitsAndCommit()
    {
        var stockItem = CreateActiveStockItem();
        var command = new SetStockLimitsCommand(StockItemId: 1, MinimumQuantity: 20, MaximumQuantity: 200);
        _stockItemRepository.GetByIdForUpdateAsync(command.StockItemId, Arg.Any<CancellationToken>()).Returns(stockItem);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stockItem.MinimumQuantity.Should().Be(20);
        stockItem.MaximumQuantity.Should().Be(200);
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
