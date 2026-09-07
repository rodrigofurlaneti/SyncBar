using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.OrderOrigin.Create;
using SyncBar.Application.Features.OrderOrigin.ExistsByName;
using SyncBar.Application.Features.OrderOrigin.GetAll;
using SyncBar.Application.Features.OrderOrigin.GetByCompanyAndBranch;
using SyncBar.Application.Features.OrderOrigin.GetById;
using SyncBar.Application.Features.OrderOrigin.GetFiltered;
using SyncBar.Application.Features.OrderOrigin.Remove;
using SyncBar.Application.Features.OrderOrigin.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class OrderOriginControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OrderOriginController _controller;

    public OrderOriginControllerTests()
    {
        _controller = new OrderOriginController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    private static OrderOriginResponse SampleResponse(long id = 1) =>
        new(id, 1, 1, "LOCAL", true, DateTime.Now);

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        var response = SampleResponse();
        _mediator.Send(Arg.Is<GetOrderOriginByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetOrderOriginByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<OrderOriginResponse>(new Error("OrderOrigin.NotFound", "Origem de pedido não encontrada.")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAll_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Any<GetAllOrderOriginsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<OrderOriginResponse>>([SampleResponse()]));

        var result = await _controller.GetAll(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAllOrderOriginsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<OrderOriginResponse>>(new Error("Generic.Invalid", "erro")));

        var result = await _controller.GetAll(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByCompanyAndBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetOrderOriginsByCompanyAndBranchQuery>(q => q.CompanyId == 1 && q.BranchId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<OrderOriginResponse>>([SampleResponse()]));

        var result = await _controller.GetByCompanyAndBranch(1, 2, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCompanyAndBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetOrderOriginsByCompanyAndBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<OrderOriginResponse>>(new Error("Generic.Invalid", "erro")));

        var result = await _controller.GetByCompanyAndBranch(1, 2, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetFiltered_Success_ShouldForwardAllFiltersAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetFilteredOrderOriginsQuery>(q =>
                q.CompanyId == 1 && q.BranchId == 2 && q.SearchTerm == "loc" && q.IsActive == true),
            Arg.Any<CancellationToken>()).Returns(Result.Success<IReadOnlyCollection<OrderOriginResponse>>([SampleResponse()]));

        var result = await _controller.GetFiltered(1, 2, "loc", true, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFiltered_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetFilteredOrderOriginsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<OrderOriginResponse>>(new Error("Generic.Invalid", "erro")));

        var result = await _controller.GetFiltered(null, null, null, null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ExistsByName_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsOrderOriginByNameQuery>(q => q.CompanyId == 1 && q.Name == "LOCAL" && q.ExcludeId == 5), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ExistsByName(1, null, "LOCAL", 5, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ExistsByName_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsOrderOriginByNameQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "erro")));

        var result = await _controller.ExistsByName(null, null, "LOCAL", null, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var command = new CreateOrderOriginCommand(1, 1, "Marketplace");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.Create(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.RouteValues!["id"].Should().Be(5L);
        created.Value.Should().Be(5L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateOrderOriginCommand(1, 1, "LOCAL");
        _mediator.Send(Arg.Any<CreateOrderOriginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("OrderOrigin.AlreadyExists", "já existe")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateOrderOriginRequest(1, 1, "Marketplace", true);
        _mediator.Send(Arg.Is<UpdateOrderOriginCommand>(c => c.Id == 1 && c.CompanyId == 1 && c.Name == "Marketplace" && c.IsActive),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateOrderOriginRequest(1, 1, "Marketplace", true);
        _mediator.Send(Arg.Any<UpdateOrderOriginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("OrderOrigin.NotFound", "não encontrada")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<RemoveOrderOriginCommand>(c => c.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RemoveOrderOriginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("OrderOrigin.NotFound", "não encontrada")));

        var result = await _controller.Delete(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
