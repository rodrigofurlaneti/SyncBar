using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Catalog.GetMenu;
using SyncBar.Application.Features.Promotions;
using SyncBar.Application.Features.Promotions.GetActive;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class CatalogControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CatalogController _controller;

    public CatalogControllerTests()
    {
        _controller = new CatalogController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetActivePromotions_Success_ShouldSendQueryWithBranchIdAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetActivePromotionsQuery>(q => q.BranchId == 5), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<ActivePromotionResponse>>([]));

        var result = await _controller.GetActivePromotions(5, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetActivePromotions_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetActivePromotionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<ActivePromotionResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetActivePromotions(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMenu_Success_ShouldSendQueryWithCompanyIdAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetMenuQuery>(q => q.CompanyId == 7), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<MenuItemResponse>>([]));

        var result = await _controller.GetMenu(7, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMenu_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetMenuQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<MenuItemResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetMenu(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
