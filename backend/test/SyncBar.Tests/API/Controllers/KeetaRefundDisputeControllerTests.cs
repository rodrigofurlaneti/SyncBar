using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Create;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Delete;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.ExistsByAfterSaleOrderId;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByAfterSaleOrderId;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByOrderId;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetPendingDisputesByBranch;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class KeetaRefundDisputeControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly KeetaRefundDisputeController _controller;

    public KeetaRefundDisputeControllerTests()
    {
        _controller = new KeetaRefundDisputeController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaRefundDisputeByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationRefundDisputeResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaRefundDisputeByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationRefundDisputeResponse>(new Error("KeetaIntegrationRefundDispute.NotFound", "disputa nao encontrada")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByAfterSaleOrderId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaRefundDisputeByAfterSaleOrderIdQuery>(q => q.AfterSaleOrderId == 10), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationRefundDisputeResponse)null!));

        var result = await _controller.GetByAfterSaleOrderId(10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByAfterSaleOrderId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaRefundDisputeByAfterSaleOrderIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationRefundDisputeResponse>(new Error("KeetaIntegrationRefundDispute.NotFound", "disputa nao encontrada")));

        var result = await _controller.GetByAfterSaleOrderId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByOrderId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaRefundDisputeByOrderIdQuery>(q => q.OrderId == "ko_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationRefundDisputeResponse)null!));

        var result = await _controller.GetByOrderId("ko_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByOrderId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaRefundDisputeByOrderIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationRefundDisputeResponse>(new Error("KeetaIntegrationRefundDispute.NotFound", "disputa nao encontrada")));

        var result = await _controller.GetByOrderId("ko_missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPendingDisputesByBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetPendingKeetaRefundDisputesByBranchQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<KeetaIntegrationRefundDisputeResponse>>([]));

        var result = await _controller.GetPendingDisputesByBranch(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPendingDisputesByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPendingKeetaRefundDisputesByBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<KeetaIntegrationRefundDisputeResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetPendingDisputesByBranch(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ExistsByAfterSaleOrderId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery>(q => q.AfterSaleOrderId == 10), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ExistsByAfterSaleOrderId(10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ExistsByAfterSaleOrderId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.ExistsByAfterSaleOrderId(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CreateKeetaIntegrationRefundDisputeCommand ValidCreateCommand() =>
        new(1, 1, "ko_1", 10, 25m, "CUSTOMER_REQUEST");

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var command = ValidCreateCommand();
        var response = new CreateKeetaIntegrationRefundDisputeResponse(5L, 1, 1, "ko_1", 10, 25m, "PENDING");
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
            .Returns(Result.Failure<CreateKeetaIntegrationRefundDisputeResponse>(new Error("KeetaIntegrationRefundDispute.AlreadyExists", "disputa ja existe")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateKeetaRefundDisputeRequest(1, true);
        _mediator.Send(Arg.Is<UpdateKeetaIntegrationRefundDisputeCommand>(c => c.Id == 1 && c.CompanyId == 1 && c.Accepted),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateKeetaRefundDisputeRequest(1, false, "code1", "motivo");
        _mediator.Send(Arg.Any<UpdateKeetaIntegrationRefundDisputeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationRefundDispute.NotFound", "disputa nao encontrada")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteKeetaIntegrationRefundDisputeCommand>(c => c.Id == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteKeetaIntegrationRefundDisputeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationRefundDispute.NotFound", "disputa nao encontrada")));

        var result = await _controller.Delete(999, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
