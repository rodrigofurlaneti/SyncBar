using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Companies.Register;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class CompaniesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CompaniesController _controller;

    public CompaniesControllerTests()
    {
        _controller = new CompaniesController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    private static RegisterCompanyCommand ValidCommand() => new(
        "Bar do Ze LTDA", "Bar do Ze", "12345678000199", null, null,
        "Loja Centro", null, null, null, null, null, null, null,
        "Jose Silva", "12345678900", "jsilva", "jose@example.com", "SenhaForte123!");

    [Fact]
    public async Task Register_Success_ShouldReturnOkWithValue()
    {
        var command = ValidCommand();
        var response = new RegisterCompanyResponse(1, 1, 1);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.Register(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task Register_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<RegisterCompanyResponse>(new Error("Company.AlreadyExists", "empresa ja cadastrada")));

        var result = await _controller.Register(command, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }
}
