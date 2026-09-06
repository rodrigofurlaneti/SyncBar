using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Promotions;
using SyncBar.Application.Features.Promotions.Create;
using SyncBar.Application.Features.Promotions.Deactivate;
using SyncBar.Application.Features.Promotions.GetByBranch;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class PromotionsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PromotionsController _controller;

    public PromotionsControllerTests()
    {
        _controller = new PromotionsController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetPromotionsByBranchQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<PromotionResponse>>([]));

        var result = await _controller.GetByBranch(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPromotionsByBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<PromotionResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranch(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static CreatePromotionCommand ValidCommand() => new(1, 1, "Happy Hour", 5, 1080, 1200, 1, 0.2m);

    [Fact]
    public async Task Create_Success_ShouldReturnOkWithValue()
    {
        var command = ValidCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(3L));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(3L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Deactivate_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeactivatePromotionCommand>(c => c.PromotionId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Deactivate(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeactivatePromotionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Promotion.NotFound", "promocao nao encontrada")));

        var result = await _controller.Deactivate(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
