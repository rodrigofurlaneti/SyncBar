using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Branches;
using SyncBar.Application.Features.Branches.Create;
using SyncBar.Application.Features.Branches.GetByCompany;
using SyncBar.Application.Features.Branches.SetSelfServiceEmployee;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class BranchesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly BranchesController _controller;

    public BranchesControllerTests()
    {
        _controller = new BranchesController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByCompany_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetBranchesByCompanyQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<BranchResponse>>([]));

        var result = await _controller.GetByCompany(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCompany_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetBranchesByCompanyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<BranchResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetByCompany(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static CreateBranchCommand ValidCommand() => new(1, "Loja Centro", null, null, null, null, null, null, null, null);

    [Fact]
    public async Task Create_Success_ShouldReturnOkWithValue()
    {
        var command = ValidCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(10L));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(10L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetSelfServiceEmployee_Success_ShouldReturnNoContent()
    {
        var command = new SetSelfServiceEmployeeCommand(1, 5);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SetSelfServiceEmployee(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetSelfServiceEmployee_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new SetSelfServiceEmployeeCommand(1, null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.SetSelfServiceEmployee(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
