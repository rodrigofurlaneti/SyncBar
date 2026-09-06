using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Shift;
using SyncBar.Application.Features.Shift.CloseShift;
using SyncBar.Application.Features.Shift.GetById;
using SyncBar.Application.Features.Shift.OpenShift;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class ShiftClosingControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ShiftClosingController _controller;

    public ShiftClosingControllerTests()
    {
        _controller = new ShiftClosingController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    private static ShiftClosingResponse SampleResponse(long id = 1) =>
        new(id, 1, 1, 1, null, DateTime.Today, null, 1, 100m, 100m, 100m, 0m, null);

    [Fact]
    public async Task GetById_Success_ShouldReturnOkWithValue()
    {
        var response = SampleResponse();
        _mediator.Send(Arg.Is<GetShiftClosingByIdQuery>(q => q.ShiftClosingId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetShiftClosingByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ShiftClosingResponse>(new Error("ShiftClosing.NotFound", "turno nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Open_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var command = new OpenShiftClosingCommand(1, 1);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(42L));

        var result = await _controller.Open(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.RouteValues!["id"].Should().Be(42L);
        created.Value.Should().Be(42L);
    }

    [Fact]
    public async Task Open_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new OpenShiftClosingCommand(1, 1);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.Open(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Close_Success_ShouldSendCommandWithIdAndReturnOkWithValue()
    {
        var request = new CloseShiftClosingRequest(2, "fechamento normal");
        var response = SampleResponse();
        _mediator.Send(Arg.Is<CloseShiftClosingCommand>(c => c.ShiftClosingId == 1 && c.ClosedByEmployeeId == 2 && c.Notes == "fechamento normal"),
            Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.Close(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task Close_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new CloseShiftClosingRequest(2, null);
        _mediator.Send(Arg.Any<CloseShiftClosingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ShiftClosingResponse>(new Error("ShiftClosing.AlreadyClosed", "turno ja fechado")));

        var result = await _controller.Close(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
