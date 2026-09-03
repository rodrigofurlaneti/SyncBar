using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Stock.GetByBranch;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Stock.GetByBranch;

public sealed class GetStockByBranchQueryHandlerTests
{
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetStockByBranchQueryHandler _handler;

    public GetStockByBranchQueryHandlerTests()
    {
        _handler = new GetStockByBranchQueryHandler(_stockItemRepository, _logRepository, _unitOfWork);
    }

    private static StockItem CreateStockItem(long productId, decimal currentQuantity, decimal minimumQuantity, decimal? maximumQuantity = null)
    {
        var item = StockItem.Create(1, productId, minimumQuantity, maximumQuantity).Value;
        if (currentQuantity > 0)
            item.Increase(currentQuantity);
        return item;
    }

    [Fact]
    public async Task Handle_NoItemsForBranch_ShouldReturnEmptyCollection()
    {
        var query = new GetStockByBranchQuery(BranchId: 1);
        _stockItemRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns(Array.Empty<StockItem>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        // Query handler não faz commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleItems_ShouldOrderByProductIdAscendingAndMapFields()
    {
        var query = new GetStockByBranchQuery(BranchId: 1);
        var itemFive = CreateStockItem(productId: 5, currentQuantity: 3, minimumQuantity: 5); // abaixo do mínimo
        var itemTwo = CreateStockItem(productId: 2, currentQuantity: 10, minimumQuantity: 5); // acima do mínimo
        _stockItemRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns([itemFive, itemTwo]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(r => r.ProductId).Should().ContainInOrder(2, 5);

        var responseTwo = result.Value.First();
        responseTwo.CurrentQuantity.Should().Be(10);
        responseTwo.IsBelowMinimum.Should().BeFalse();

        var responseFive = result.Value.Last();
        responseFive.CurrentQuantity.Should().Be(3);
        responseFive.IsBelowMinimum.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ItemAtExactMinimum_ShouldMapIsBelowMinimumFalse()
    {
        var query = new GetStockByBranchQuery(BranchId: 1);
        var item = CreateStockItem(productId: 1, currentQuantity: 5, minimumQuantity: 5); // igual ao mínimo — comparação estrita (<)
        _stockItemRepository.GetByBranchAsync(query.BranchId, Arg.Any<CancellationToken>()).Returns([item]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Single().IsBelowMinimum.Should().BeFalse();
    }
}
