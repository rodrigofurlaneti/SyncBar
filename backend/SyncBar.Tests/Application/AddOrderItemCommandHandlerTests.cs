using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class AddOrderItemCommandHandlerTests
{
    private readonly ICustomerOrderRepository _orderRepository = Substitute.For<ICustomerOrderRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ShouldFreezeMenuPriceOnItem()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        var product = Product.Create(1, 1, 1, "Cerveja Pilsen 600ml", null, null, 14.90m, 6.50m, true, null).Value;
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(product);

        var handler = new AddOrderItemCommandHandler(_orderRepository, _productRepository, _unitOfWork);
        var result = await handler.Handle(new AddOrderItemCommand(1, 5, 2, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(1);
        order.Items.First().UnitPrice.Should().Be(14.90m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownProduct_ShouldFail()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        _orderRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _productRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var handler = new AddOrderItemCommandHandler(_orderRepository, _productRepository, _unitOfWork);
        var result = await handler.Handle(new AddOrderItemCommand(1, 99, 1, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }
}
