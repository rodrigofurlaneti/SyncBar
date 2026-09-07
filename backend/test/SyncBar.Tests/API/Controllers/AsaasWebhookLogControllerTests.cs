using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Delete;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetById;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetUnprocessedLogs;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.HasAlreadyProcessedEvent;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Update;
using SyncBar.Domain.Enums;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class AsaasWebhookLogControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AsaasWebhookLogController _controller;

    public AsaasWebhookLogControllerTests()
    {
        _controller = new AsaasWebhookLogController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAsaasWebhookLogByIdQuery>(q => q.Id == 1 && q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((AsaasWebhookLogResponse)null!));

        var result = await _controller.GetById(1, 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAsaasWebhookLogByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AsaasWebhookLogResponse>(new Error("AsaasWebhookLog.NotFound", "log nao encontrado")));

        var result = await _controller.GetById(999, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByPaymentId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAsaasWebhookLogsByPaymentIdQuery>(q => q.CompanyId == 1 && q.PaymentId == "pay_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<AsaasWebhookLogResponse>>([]));

        var result = await _controller.GetByPaymentId("pay_1", 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByPaymentId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAsaasWebhookLogsByPaymentIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<AsaasWebhookLogResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetByPaymentId("pay_missing", 1, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetUnprocessed_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetUnprocessedAsaasWebhookLogsQuery>(q => q.CompanyId == 1 && q.Limit == 50), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<AsaasWebhookLogResponse>>([]));

        var result = await _controller.GetUnprocessed(1, CancellationToken.None, 50);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUnprocessed_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetUnprocessedAsaasWebhookLogsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<AsaasWebhookLogResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetUnprocessed(999, CancellationToken.None, 50);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task HasAlreadyProcessedEvent_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<HasAlreadyProcessedEventQuery>(q => q.AsaasEventId == "evt_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.HasAlreadyProcessedEvent("evt_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task HasAlreadyProcessedEvent_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<HasAlreadyProcessedEventQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.HasAlreadyProcessedEvent("", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateWebhookLogStatusRequest(1, WebhookLogStatus.Processed, null);
        _mediator.Send(Arg.Is<UpdateAsaasWebhookLogStatusCommand>(c =>
                c.Id == 1 && c.CompanyId == 1 && c.Status == WebhookLogStatus.Processed && c.ErrorMessage == null),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.UpdateStatus(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateStatus_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateWebhookLogStatusRequest(1, WebhookLogStatus.Failed, "erro ao processar");
        _mediator.Send(Arg.Any<UpdateAsaasWebhookLogStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasWebhookLog.NotFound", "log nao encontrado")));

        var result = await _controller.UpdateStatus(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_MissingCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new UpdateWebhookLogStatusRequest(null, WebhookLogStatus.Processed, null);

        var result = await _controller.UpdateStatus(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateAsaasWebhookLogStatusCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatus_MissingStatus_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new UpdateWebhookLogStatusRequest(1, null, null);

        var result = await _controller.UpdateStatus(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateAsaasWebhookLogStatusCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteAsaasWebhookLogCommand>(c => c.Id == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteAsaasWebhookLogCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasWebhookLog.NotFound", "log nao encontrado")));

        var result = await _controller.Delete(999, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
