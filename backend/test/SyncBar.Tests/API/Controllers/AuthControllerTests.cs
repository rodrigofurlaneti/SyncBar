using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Auth;
using SyncBar.Application.Features.Auth.CustomerLogin;
using SyncBar.Application.Features.Auth.Login;
using SyncBar.Application.Features.Auth.Refresh;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class AuthControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<AuthController> _logger = NullLogger<AuthController>.Instance;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_mediator, _logRepository, _unitOfWork, _logger);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    private static LoginResponse SampleLoginResponse() => new(
        "access-tok", DateTime.UtcNow.AddHours(1), "refresh-tok", DateTime.UtcNow.AddDays(7), "jsilva", 1, null);

    [Fact]
    public async Task Login_Success_ShouldEnrichCommandWithIpAndUserAgentAndReturnOk()
    {
        _controller.ControllerContext.HttpContext.Request.Headers.UserAgent = "TestAgent/1.0";
        var response = SampleLoginResponse();
        _mediator.Send(Arg.Is<LoginCommand>(c => c.UserName == "jsilva" && c.UserAgent == "TestAgent/1.0"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.Login(new LoginCommand("jsilva", "Senha123!"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => l.IsSuccess && l.ClassName == nameof(AuthController)), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_Failure_ShouldReturnMappedErrorResultAndLogFailure()
    {
        _mediator.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<LoginResponse>(new Error("Auth.InvalidCredentials", "credenciais invalidas")));

        var result = await _controller.Login(new LoginCommand("jsilva", "senha-errada"), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => !l.IsSuccess), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_LogPersistenceThrows_ShouldBeSwallowedAndStillReturnResult()
    {
        _logRepository.AddAsync(Arg.Any<LogTracker>(), Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(new Exception("falha ao gravar log"));
        _mediator.Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(SampleLoginResponse()));

        var result = await _controller.Login(new LoginCommand("jsilva", "Senha123!"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    private static CustomerLoginResponse SampleCustomerLoginResponse() => new(
        "access-tok", DateTime.UtcNow.AddHours(1), "refresh-tok", DateTime.UtcNow.AddDays(7), "Cliente Teste", 1, 1);

    [Fact]
    public async Task CustomerLogin_Success_ShouldEnrichCommandWithIpAndUserAgentAndReturnOk()
    {
        var response = SampleCustomerLoginResponse();
        _mediator.Send(Arg.Is<CustomerLoginCommand>(c => c.Email == "cliente@example.com"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.CustomerLogin(new CustomerLoginCommand("cliente@example.com", "Senha123!", 1, null), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => l.MethodName == nameof(AuthController.CustomerLogin)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CustomerLogin_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<CustomerLoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CustomerLoginResponse>(new Error("Auth.InvalidCredentials", "credenciais invalidas")));

        var result = await _controller.CustomerLogin(new CustomerLoginCommand("cliente@example.com", "errada", 1, null), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CustomerLogin_LogPersistenceThrows_ShouldBeSwallowedAndStillReturnResult()
    {
        _logRepository.AddAsync(Arg.Any<LogTracker>(), Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(new Exception("falha ao gravar log"));
        _mediator.Send(Arg.Any<CustomerLoginCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(SampleCustomerLoginResponse()));

        var result = await _controller.CustomerLogin(new CustomerLoginCommand("cliente@example.com", "Senha123!", 1, null), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Refresh_Success_ShouldReturnOkWithValue()
    {
        var command = new RefreshTokenCommand("refresh-tok");
        var response = SampleLoginResponse();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.Refresh(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task Refresh_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new RefreshTokenCommand("invalid-tok");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<LoginResponse>(new Error("Auth.InvalidRefreshToken", "token invalido")));

        var result = await _controller.Refresh(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
