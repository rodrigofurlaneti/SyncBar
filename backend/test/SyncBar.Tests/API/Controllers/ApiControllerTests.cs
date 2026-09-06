using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SyncBar.API.Controllers;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

// ApiController é abstrato — exercitado aqui via um subtipo mínimo que só expõe os métodos
// protegidos (HandleFailure/ExecuteWithLogAsync/CreateProblemDetails) publicamente para teste.
// Cobrindo a lógica compartilhada aqui uma única vez evita repetir os mesmos cenários de
// mapeamento de erro/log em cada um dos ~50 controllers concretos, que só precisam confirmar seu
// próprio diferencial (qual comando/query é enviado ao Mediator).
internal sealed class TestableApiController(IMediator mediator) : ApiController(mediator)
{
    public IActionResult CallHandleFailure(Result result) => HandleFailure(result);

    public Task<IActionResult> CallExecuteWithLogAsync(
        ILogTrackerRepository logRepository, IUnitOfWork unitOfWork, string className, string methodName, Func<Task<IActionResult>> action)
        => ExecuteWithLogAsync(logRepository, unitOfWork, className, methodName, action);

    public Task<IActionResult> CallExecuteWithLogAsync(string className, string methodName, Func<Task<IActionResult>> action)
        => ExecuteWithLogAsync(className, methodName, action);
}

public sealed class ApiControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TestableApiController _controller;

    public ApiControllerTests()
    {
        _controller = new TestableApiController(_mediator);
        AttachHttpContext(_controller);
    }

    private static void AttachHttpContext(ControllerBase controller, IServiceProvider? services = null, string? userId = null)
    {
        var httpContext = new DefaultHttpContext();
        if (services is not null)
            httpContext.RequestServices = services;
        if (userId is not null)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth"));
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Theory]
    [InlineData("Customer.NotFound", typeof(NotFoundObjectResult))]
    [InlineData("Asaas.AlreadyExists", typeof(ConflictObjectResult))]
    [InlineData("Product.Duplicate", typeof(ConflictObjectResult))]
    [InlineData("Generic.ValidationError", typeof(BadRequestObjectResult))]
    public void HandleFailure_MapsErrorCodeSuffixToExpectedHttpResult(string errorCode, Type expectedResultType)
    {
        var result = Result.Failure(new Error(errorCode, "mensagem de erro"));

        var actionResult = _controller.CallHandleFailure(result);

        actionResult.Should().BeOfType(expectedResultType);
        var problemDetails = ((ObjectResult)actionResult).Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be(errorCode);
        problemDetails.Detail.Should().Be("mensagem de erro");
    }

    [Fact]
    public async Task ExecuteWithLogAsync_ActionReturnsOk_ShouldLogSuccessAndCommit()
    {
        var result = await _controller.CallExecuteWithLogAsync(_logRepository, _unitOfWork, "TestClass", "TestMethod", () => Task.FromResult<IActionResult>(new OkObjectResult("valor")));

        result.Should().BeOfType<OkObjectResult>();
        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l =>
            l.IsSuccess && l.ClassName == "TestClass" && l.MethodName == "TestMethod" && l.DirectoryName == "Controllers"));
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task ExecuteWithLogAsync_ActionReturnsNoContent_ShouldLogSuccess()
    {
        await _controller.CallExecuteWithLogAsync(_logRepository, _unitOfWork, "TestClass", "TestMethod", () => Task.FromResult<IActionResult>(new NoContentResult()));

        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => l.IsSuccess), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteWithLogAsync_ActionReturnsCreatedAtAction_ShouldLogSuccess()
    {
        await _controller.CallExecuteWithLogAsync(_logRepository, _unitOfWork, "TestClass", "TestMethod",
            () => Task.FromResult<IActionResult>(new CreatedAtActionResult("Get", "Test", null, "valor")));

        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => l.IsSuccess), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteWithLogAsync_ActionReturnsProblemDetailsFailure_ShouldLogFailureWithTitleAndDetail()
    {
        var problem = new ProblemDetails { Title = "Order.NotFound", Detail = "pedido nao encontrado" };

        await _controller.CallExecuteWithLogAsync(_logRepository, _unitOfWork, "TestClass", "TestMethod",
            () => Task.FromResult<IActionResult>(new NotFoundObjectResult(problem)));

        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l =>
            !l.IsSuccess && l.Message == "Falha na regra de negócio." && l.ErrorMessage == "Order.NotFound: pedido nao encontrado"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteWithLogAsync_ActionReturnsProblemDetailsWithoutDetail_ShouldLogOnlyTitle()
    {
        var problem = new ProblemDetails { Title = "Order.NotFound" };

        await _controller.CallExecuteWithLogAsync(_logRepository, _unitOfWork, "TestClass", "TestMethod",
            () => Task.FromResult<IActionResult>(new NotFoundObjectResult(problem)));

        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => l.ErrorMessage == "Order.NotFound"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteWithLogAsync_ActionReturnsPlainObjectFailure_ShouldExtractTitleAndDetailViaReflection()
    {
        var anonymousError = new { title = "Custom.Error", detail = "detalhe customizado" };

        await _controller.CallExecuteWithLogAsync(_logRepository, _unitOfWork, "TestClass", "TestMethod",
            () => Task.FromResult<IActionResult>(new BadRequestObjectResult(anonymousError)));

        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => l.ErrorMessage == "Custom.Error: detalhe customizado"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteWithLogAsync_ActionThrows_ShouldLogAndRethrow()
    {
        var act = () => _controller.CallExecuteWithLogAsync(_logRepository, _unitOfWork, "TestClass", "TestMethod",
            () => throw new InvalidOperationException("falha inesperada"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _logRepository.Received(1).AddAsync(
            Arg.Is<LogTracker>(l => !l.IsSuccess && l.Message == "Erro interno no servidor." && l.ErrorMessage == "falha inesperada"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync();
    }

    [Fact]
    public async Task ExecuteWithLogAsync_LogPersistenceThrows_ShouldBeSwallowedAndStillReturnActionResult()
    {
        _logRepository.AddAsync(Arg.Any<LogTracker>(), Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(new Exception("falha ao gravar log"));

        var result = await _controller.CallExecuteWithLogAsync(_logRepository, _unitOfWork, "TestClass", "TestMethod",
            () => Task.FromResult<IActionResult>(new OkObjectResult("valor")));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ExecuteWithLogAsync_WithAuthenticatedUser_ShouldCaptureAppUserIdFromClaim()
    {
        AttachHttpContext(_controller, userId: "42");

        await _controller.CallExecuteWithLogAsync(_logRepository, _unitOfWork, "TestClass", "TestMethod",
            () => Task.FromResult<IActionResult>(new OkObjectResult("valor")));

        await _logRepository.Received(1).AddAsync(Arg.Is<LogTracker>(l => l.AppUserId == 42), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteWithLogAsync_ThreeArgOverload_ShouldResolveDependenciesFromRequestServices()
    {
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ILogTrackerRepository)).Returns(_logRepository);
        services.GetService(typeof(IUnitOfWork)).Returns(_unitOfWork);
        AttachHttpContext(_controller, services);

        var result = await _controller.CallExecuteWithLogAsync("TestClass", "TestMethod", () => Task.FromResult<IActionResult>(new OkObjectResult("valor")));

        result.Should().BeOfType<OkObjectResult>();
        await _logRepository.Received(1).AddAsync(Arg.Any<LogTracker>(), Arg.Any<CancellationToken>());
    }
}
