using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.Storefront;
using SyncBar.Application.Features.Storefront.AddOrder;
using SyncBar.Application.Features.Storefront.GetBranchMenu;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class StorefrontControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly StorefrontController _controller;

    public StorefrontControllerTests()
    {
        _controller = new StorefrontController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetBranchMenu_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetBranchMenuQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((BranchMenuResponse)null!));

        var result = await _controller.GetBranchMenu(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBranchMenu_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetBranchMenuQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<BranchMenuResponse>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetBranchMenu(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateOrder_Success_ShouldMapItemsAndReturnOkWithWrappedOrderId()
    {
        var request = new AddWebStorefrontOrderRequest(
            null, "Cliente Teste", null, null,
            [new WebStorefrontItemRequest(1, 2, "sem cebola", [new OrderItemComplementSelection(1, 1)])]);
        _mediator.Send(Arg.Is<AddWebStorefrontOrderCommand>(c =>
                c.BranchId == 1 && c.CustomerName == "Cliente Teste" && c.Items.Count == 1
                && c.Items.First().ProductId == 1 && c.Items.First().Complements!.Count == 1),
            Arg.Any<CancellationToken>()).Returns(Result.Success(77L));

        var result = await _controller.CreateOrder(1, request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { orderId = 77L });
    }

    [Fact]
    public async Task CreateOrder_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AddWebStorefrontOrderRequest(null, "Cliente Teste", null, null, []);
        _mediator.Send(Arg.Any<AddWebStorefrontOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.CreateOrder(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
