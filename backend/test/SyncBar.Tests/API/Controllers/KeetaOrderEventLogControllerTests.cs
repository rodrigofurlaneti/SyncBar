using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Create;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Delete;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.ExistsByEventId;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetAllByOrderId;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetByEventId;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetUnprocessedEvents;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class KeetaOrderEventLogControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly KeetaOrderEventLogController _controller;

    public KeetaOrderEventLogControllerTests()
    {
        _controller = new KeetaOrderEventLogController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaOrderEventLogByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationOrderEventLogResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaOrderEventLogByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationOrderEventLogResponse>(new Error("KeetaIntegrationOrderEventLog.NotFound", "evento nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByEventId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaOrderEventLogByEventIdQuery>(q => q.EventId == "evt_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationOrderEventLogResponse)null!));

        var result = await _controller.GetByEventId("evt_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByEventId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaOrderEventLogByEventIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationOrderEventLogResponse>(new Error("KeetaIntegrationOrderEventLog.NotFound", "evento nao encontrado")));

        var result = await _controller.GetByEventId("evt_missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAllByOrderId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAllKeetaOrderEventLogsByOrderIdQuery>(q => q.OrderId == "ko_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<KeetaIntegrationOrderEventLogResponse>>([]));

        var result = await _controller.GetAllByOrderId("ko_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllByOrderId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAllKeetaOrderEventLogsByOrderIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<KeetaIntegrationOrderEventLogResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetAllByOrderId("ko_missing", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetUnprocessed_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Any<GetUnprocessedKeetaOrderEventLogsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<KeetaIntegrationOrderEventLogResponse>>([]));

        var result = await _controller.GetUnprocessed(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUnprocessed_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetUnprocessedKeetaOrderEventLogsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<KeetaIntegrationOrderEventLogResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetUnprocessed(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ExistsByEventId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsKeetaOrderEventLogByEventIdQuery>(q => q.EventId == "evt_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ExistsByEventId("evt_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ExistsByEventId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsKeetaOrderEventLogByEventIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.ExistsByEventId("", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CreateKeetaIntegrationOrderEventLogCommand ValidCreateCommand() =>
        new(1, 1, "evt_1", "ko_1", "ORDER_CONFIRMED", "{}", DateTime.UtcNow);

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var command = ValidCreateCommand();
        var response = new CreateKeetaIntegrationOrderEventLogResponse(5L, 1, 1, "evt_1", "ko_1", "ORDER_CONFIRMED");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.Create(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.RouteValues!["id"].Should().Be(5L);
        created.Value.Should().Be(response);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCreateCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CreateKeetaIntegrationOrderEventLogResponse>(new Error("KeetaIntegrationOrderEventLog.AlreadyExists", "evento ja existe")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateKeetaOrderEventLogRequest(1, true);
        _mediator.Send(Arg.Is<UpdateKeetaIntegrationOrderEventLogCommand>(c => c.Id == 1 && c.CompanyId == 1 && c.MarkAsProcessed),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateKeetaOrderEventLogRequest(1);
        _mediator.Send(Arg.Any<UpdateKeetaIntegrationOrderEventLogCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrderEventLog.NotFound", "evento nao encontrado")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteKeetaIntegrationOrderEventLogCommand>(c => c.Id == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteKeetaIntegrationOrderEventLogCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationOrderEventLog.NotFound", "evento nao encontrado")));

        var result = await _controller.Delete(999, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
