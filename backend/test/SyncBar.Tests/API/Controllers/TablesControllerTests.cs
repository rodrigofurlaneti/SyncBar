using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Tables;
using SyncBar.Application.Features.Tables.GenerateQrToken;
using SyncBar.Application.Features.Tables.GetByBranch;
using SyncBar.Application.Features.Tables.SetReadingValidation;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class TablesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TablesController _controller;

    public TablesControllerTests()
    {
        _controller = new TablesController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetTablesByBranchQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<TableResponse>>([]));

        var result = await _controller.GetByBranch(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetTablesByBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<TableResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranch(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GenerateQrToken_Success_ShouldReturnOkWithWrappedToken()
    {
        var token = Guid.NewGuid();
        _mediator.Send(Arg.Is<GenerateTableQrTokenCommand>(c => c.DiningTableId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(token));

        var result = await _controller.GenerateQrToken(1, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { token });
    }

    [Fact]
    public async Task GenerateQrToken_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GenerateTableQrTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Guid>(new Error("DiningTable.NotFound", "mesa nao encontrada")));

        var result = await _controller.GenerateQrToken(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetReadingValidation_Success_ShouldSendCommandWithAllFlagsAndReturnNoContent()
    {
        var request = new SetReadingValidationRequest(true, false, true);
        _mediator.Send(Arg.Is<SetDiningTableReadingValidationCommand>(c =>
                c.DiningTableId == 1 && c.IsCameraInputEnabled && !c.IsBarcodeEnabled && c.IsQrCodeEnabled),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SetReadingValidation(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetReadingValidation_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new SetReadingValidationRequest(false, false, false);
        _mediator.Send(Arg.Any<SetDiningTableReadingValidationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("DiningTable.NotFound", "mesa nao encontrada")));

        var result = await _controller.SetReadingValidation(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
