using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Access;
using SyncBar.Application.Features.Access.GetFeatures;
using SyncBar.Application.Features.Access.GetJobTitleFeatures;
using SyncBar.Application.Features.Access.GetMyFeatures;
using SyncBar.Application.Features.Access.GetUserFeatures;
using SyncBar.Application.Features.Access.SetJobTitleFeatures;
using SyncBar.Application.Features.Access.SetUserFeatures;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class AccessControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AccessController _controller;

    public AccessControllerTests()
    {
        _controller = new AccessController(_mediator);
        AttachHttpContext();
    }

    // AccessController usa a sobrecarga de 3 argumentos de ExecuteWithLogAsync (resolve
    // ILogTrackerRepository/IUnitOfWork via HttpContext.RequestServices) — precisa de um
    // IServiceProvider configurado, diferente do resto dos controllers deste projeto.
    private void AttachHttpContext(string? userId = "1", IEnumerable<string>? roles = null)
    {
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ILogTrackerRepository)).Returns(_logRepository);
        services.GetService(typeof(IUnitOfWork)).Returns(_unitOfWork);

        var claims = new List<Claim>();
        if (userId is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        foreach (var role in roles ?? [])
            claims.Add(new Claim(ClaimTypes.Role, role));

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")),
        };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task GetMyFeatures_AuthenticatedNonManager_ShouldSendQueryWithIsManagerFalseAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetMyFeaturesQuery>(q => q.AppUserId == 1 && !q.IsManager), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new MyFeaturesResponse(false, [])));

        var result = await _controller.GetMyFeatures(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyFeatures_Manager_ShouldSendQueryWithIsManagerTrue()
    {
        AttachHttpContext(roles: ["Gerente"]);
        _mediator.Send(Arg.Any<GetMyFeaturesQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(new MyFeaturesResponse(false, [])));

        await _controller.GetMyFeatures(CancellationToken.None);

        await _mediator.Received(1).Send(Arg.Is<GetMyFeaturesQuery>(q => q.IsManager), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyFeatures_NoUserIdClaim_ShouldReturnUnauthorizedWithoutCallingMediator()
    {
        AttachHttpContext(userId: null);

        var result = await _controller.GetMyFeatures(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<GetMyFeaturesQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyFeatures_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetMyFeaturesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<MyFeaturesResponse>(new Error("AppUser.NotFound", "usuario nao encontrado")));

        var result = await _controller.GetMyFeatures(CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetFeatures_Success_ShouldReturnOk()
    {
        _mediator.Send(Arg.Any<GetFeaturesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<FeatureResponse>>([]));

        var result = await _controller.GetFeatures(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFeatures_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetFeaturesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<FeatureResponse>>(new Error("Generic.Error", "erro")));

        var result = await _controller.GetFeatures(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetJobTitleFeatures_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetJobTitleFeaturesQuery>(q => q.JobTitleId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<long>>([1, 2]));

        var result = await _controller.GetJobTitleFeatures(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetJobTitleFeatures_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetJobTitleFeaturesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<long>>(new Error("JobTitle.NotFound", "cargo nao encontrado")));

        var result = await _controller.GetJobTitleFeatures(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetJobTitleFeatures_Success_ShouldSendCommandWithIdAndFeatureIdsAndReturnNoContent()
    {
        var request = new SetFeaturesRequest([1, 2, 3]);
        _mediator.Send(Arg.Is<SetJobTitleFeaturesCommand>(c => c.JobTitleId == 1 && c.FeatureIds.SequenceEqual(new List<long> { 1, 2, 3 })),
            Arg.Any<CancellationToken>()).Returns(Result.Success(Result.Success()));

        var result = await _controller.SetJobTitleFeatures(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetJobTitleFeatures_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new SetFeaturesRequest([]);
        _mediator.Send(Arg.Any<SetJobTitleFeaturesCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Result>(new Error("JobTitle.NotFound", "cargo nao encontrado")));

        var result = await _controller.SetJobTitleFeatures(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUserFeatures_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetUserFeaturesQuery>(q => q.AppUserId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<long>>([1]));

        var result = await _controller.GetUserFeatures(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserFeatures_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetUserFeaturesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<long>>(new Error("AppUser.NotFound", "usuario nao encontrado")));

        var result = await _controller.GetUserFeatures(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetUserFeatures_Success_ShouldSendCommandWithIdAndFeatureIdsAndReturnNoContent()
    {
        var request = new SetFeaturesRequest([4, 5]);
        _mediator.Send(Arg.Is<SetUserFeaturesCommand>(c => c.AppUserId == 1 && c.FeatureIds.SequenceEqual(new long[] { 4, 5 })),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SetUserFeatures(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetUserFeatures_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new SetFeaturesRequest([]);
        _mediator.Send(Arg.Any<SetUserFeaturesCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AppUser.NotFound", "usuario nao encontrado")));

        var result = await _controller.SetUserFeatures(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
