using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.HandleOAuthCallback;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.RefreshAccessToken;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.RequestAuthorizationUrl;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class KeetaOAuthControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly KeetaOAuthController _controller;

    public KeetaOAuthControllerTests()
    {
        _controller = new KeetaOAuthController(_mediator, _logRepository, _unitOfWork);
    }

    private void AttachContext()
    {
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ILogTrackerRepository)).Returns(_logRepository);
        services.GetService(typeof(IUnitOfWork)).Returns(_unitOfWork);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = services };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task GetAuthorizationUrl_Success_ShouldSendCommandAndReturnOk()
    {
        ControllerTestHelpers.AttachHttpContext(_controller);
        var response = new RequestKeetaAuthorizationUrlResponse("https://keeta.example/authorize");
        _mediator.Send(Arg.Is<RequestKeetaAuthorizationUrlCommand>(c => c.CompanyId == 1 && c.BranchId == 2 && c.RedirectUri == "https://app.example/callback"),
            Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.GetAuthorizationUrl(1, 2, "https://app.example/callback", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetAuthorizationUrl_Failure_ShouldReturnMappedErrorResult()
    {
        ControllerTestHelpers.AttachHttpContext(_controller);
        _mediator.Send(Arg.Any<RequestKeetaAuthorizationUrlCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RequestKeetaAuthorizationUrlResponse>(new Error("Keeta.Failed", "falha ao gerar url")));

        var result = await _controller.GetAuthorizationUrl(1, 2, "https://app.example/callback", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Callback_Success_ShouldRedirectWithSuccessQueryParam()
    {
        AttachContext();
        var response = new HandleKeetaOAuthCallbackResponse(1, "auth-1", true, [100]);
        _mediator.Send(Arg.Any<HandleKeetaOAuthCallbackCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.Callback(1, 2, "auth-1", null, null, null, CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("/integracoes/keeta?keetaAuth=success");
    }

    [Fact]
    public async Task Callback_Failure_ShouldRedirectWithErrorQueryParam()
    {
        AttachContext();
        _mediator.Send(Arg.Any<HandleKeetaOAuthCallbackCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<HandleKeetaOAuthCallbackResponse>(new Error("Keeta.Failed", "falha na autorizacao")));

        var result = await _controller.Callback(1, 2, "auth-1", null, null, null, CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("/integracoes/keeta?keetaAuth=error");
    }

    [Fact]
    public async Task RefreshToken_Success_ShouldSendCommandAndReturnOk()
    {
        ControllerTestHelpers.AttachHttpContext(_controller);
        var response = new RefreshKeetaAccessTokenResponse(DateTime.UtcNow.AddHours(1));
        _mediator.Send(Arg.Is<RefreshKeetaAccessTokenCommand>(c => c.CompanyId == 1 && c.BranchId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.RefreshToken(new RefreshKeetaAccessTokenRequest(1, 2), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task RefreshToken_Failure_ShouldReturnMappedErrorResult()
    {
        ControllerTestHelpers.AttachHttpContext(_controller);
        _mediator.Send(Arg.Any<RefreshKeetaAccessTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RefreshKeetaAccessTokenResponse>(new Error("Keeta.NoToken", "sem token")));

        var result = await _controller.RefreshToken(new RefreshKeetaAccessTokenRequest(1, 2), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
