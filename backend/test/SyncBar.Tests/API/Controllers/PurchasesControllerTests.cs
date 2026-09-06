using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Purchases;
using SyncBar.Application.Features.Purchases.GetByBranch;
using SyncBar.Application.Features.Purchases.Register;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class PurchasesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PurchasesController _controller;

    public PurchasesControllerTests()
    {
        _controller = new PurchasesController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetPurchasesByBranchQuery>(q => q.BranchId == 5), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<PurchaseResponse>>([]));

        var result = await _controller.GetByBranch(5, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPurchasesByBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<PurchaseResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranch(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static RegisterPurchaseCommand ValidCommand() => new(
        1, 1, 1, "DOC-1", DateTime.Today, null, [new PurchaseItemInput(1, 10m, 5m)]);

    [Fact]
    public async Task Register_Success_ShouldReturnOkWithValue()
    {
        var command = ValidCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(42L));

        var result = await _controller.Register(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(42L);
    }

    [Fact]
    public async Task Register_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Supplier.NotFound", "fornecedor nao encontrado")));

        var result = await _controller.Register(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
