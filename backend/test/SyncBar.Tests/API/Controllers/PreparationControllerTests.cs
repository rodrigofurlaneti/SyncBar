using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Orders.UpdateItemStatus;
using SyncBar.Application.Features.Preparation.GetQueue;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class PreparationControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PreparationController _controller;

    public PreparationControllerTests()
    {
        _controller = new PreparationController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetQueue_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetPreparationQueueQuery>(q => q.BranchId == 5), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<PreparationTicketResponse>>([]));

        var result = await _controller.GetQueue(5, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetQueue_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPreparationQueueQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<PreparationTicketResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetQueue(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateItemStatus_NonManagerUser_Success_ShouldSendCommandWithIsManagerFalseAndReturnNoContent()
    {
        var request = new UpdateOrderItemStatusRequest(2, 10);
        _mediator.Send(Arg.Is<UpdateOrderItemStatusCommand>(c =>
                c.CustomerOrderId == 1 && c.OrderItemId == 3 && c.OrderItemStatusId == 2 && c.ActorEmployeeId == 10 && !c.IsManager),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.UpdateItemStatus(1, 3, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateItemStatus_ManagerUser_ShouldSendCommandWithIsManagerTrue()
    {
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Gerente")], "TestAuth")),
        };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var request = new UpdateOrderItemStatusRequest(2, null);
        _mediator.Send(Arg.Any<UpdateOrderItemStatusCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        await _controller.UpdateItemStatus(1, 3, request, CancellationToken.None);

        await _mediator.Received(1).Send(Arg.Is<UpdateOrderItemStatusCommand>(c => c.IsManager), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateItemStatus_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateOrderItemStatusRequest(2, null);
        _mediator.Send(Arg.Any<UpdateOrderItemStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("OrderItem.NotFound", "item nao encontrado")));

        var result = await _controller.UpdateItemStatus(1, 3, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
