using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Dining.Area;
using SyncBar.Application.Features.Dining.Area.Create;
using SyncBar.Application.Features.Dining.Area.GetByBranchId;
using SyncBar.Application.Features.Dining.Area.GetById;
using SyncBar.Application.Features.Dining.Area.Update;
using SyncBar.Application.Features.Dining.Assignment;
using SyncBar.Application.Features.Dining.Assignment.Create;
using SyncBar.Application.Features.Dining.Assignment.End;
using SyncBar.Application.Features.Dining.Assignment.GetActiveByDiningAreaId;
using SyncBar.Application.Features.Dining.Assignment.GetActiveByEmployeeId;
using SyncBar.Application.Features.Dining.Messages;
using SyncBar.Application.Features.Dining.Messages.Create;
using SyncBar.Application.Features.Dining.Messages.GetWaiterMessagesByBranch;
using SyncBar.Application.Features.Dining.Table;
using SyncBar.Application.Features.Dining.Table.Create;
using SyncBar.Application.Features.Dining.Table.Deactivate;
using SyncBar.Application.Features.Dining.Table.GetByDiningAreaId;
using SyncBar.Application.Features.Dining.Table.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class DiningAreasControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DiningAreasController _controller;

    public DiningAreasControllerTests()
    {
        _controller = new DiningAreasController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetDiningAreasByBranchQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<DiningAreaListResponse>>([]));

        var result = await _controller.GetByBranch(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetDiningAreasByBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<DiningAreaListResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranch(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetDiningAreaByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((DiningAreaResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetDiningAreaByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<DiningAreaResponse>(new Error("DiningArea.NotFound", "area nao encontrada")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateArea_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var command = new CreateDiningAreaCommand(1, "Salao Principal");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.CreateArea(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.RouteValues!["id"].Should().Be(5L);
    }

    [Fact]
    public async Task CreateArea_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateDiningAreaCommand(1, "");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("DiningArea.EmptyName", "nome obrigatorio")));

        var result = await _controller.CreateArea(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateArea_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateDiningAreaRequest("Salao Novo");
        _mediator.Send(Arg.Is<UpdateDiningAreaCommand>(c => c.Id == 1 && c.Name == "Salao Novo"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.UpdateArea(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateArea_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateDiningAreaRequest("Salao Novo");
        _mediator.Send(Arg.Any<UpdateDiningAreaCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("DiningArea.NotFound", "area nao encontrada")));

        var result = await _controller.UpdateArea(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTablesByArea_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetDiningAreaTablesByAreaIdQuery>(q => q.DiningAreaId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<DiningAreaTableListResponse>>([]));

        var result = await _controller.GetTablesByArea(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTablesByArea_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetDiningAreaTablesByAreaIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<DiningAreaTableListResponse>>(new Error("DiningArea.NotFound", "area nao encontrada")));

        var result = await _controller.GetTablesByArea(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AssignTableToArea_Success_ShouldSendCommandWithAreaIdAndReturnOkWithValue()
    {
        var request = new AssignTableRequest(2);
        _mediator.Send(Arg.Is<CreateDiningAreaTableCommand>(c => c.DiningAreaId == 1 && c.DiningTableId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success(9L));

        var result = await _controller.AssignTableToArea(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(9L);
    }

    [Fact]
    public async Task AssignTableToArea_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AssignTableRequest(2);
        _mediator.Send(Arg.Any<CreateDiningAreaTableCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("DiningTable.NotFound", "mesa nao encontrada")));

        var result = await _controller.AssignTableToArea(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateTableAssignment_Success_ShouldSendCommandWithAssignmentIdAndReturnNoContent()
    {
        var request = new UpdateTableAssignmentRequest(1, 2);
        _mediator.Send(Arg.Is<UpdateDiningAreaTableCommand>(c => c.Id == 5 && c.DiningAreaId == 1 && c.DiningTableId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.UpdateTableAssignment(5, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateTableAssignment_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateTableAssignmentRequest(1, 2);
        _mediator.Send(Arg.Any<UpdateDiningAreaTableCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("DiningAreaTable.NotFound", "vinculo nao encontrado")));

        var result = await _controller.UpdateTableAssignment(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RemoveTableFromArea_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeactivateDiningAreaTableCommand>(c => c.Id == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.RemoveTableFromArea(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveTableFromArea_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeactivateDiningAreaTableCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("DiningAreaTable.NotFound", "vinculo nao encontrado")));

        var result = await _controller.RemoveTableFromArea(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetActiveAssignmentsByArea_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetActiveAssignmentsByDiningAreaIdQuery>(q => q.DiningAreaId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<DiningAreaAssignmentListResponse>>([]));

        var result = await _controller.GetActiveAssignmentsByArea(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetActiveAssignmentsByArea_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetActiveAssignmentsByDiningAreaIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<DiningAreaAssignmentListResponse>>(new Error("DiningArea.NotFound", "area nao encontrada")));

        var result = await _controller.GetActiveAssignmentsByArea(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetActiveAssignmentsByEmployee_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetActiveAssignmentsByEmployeeIdQuery>(q => q.EmployeeId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<DiningAreaAssignmentListResponse>>([]));

        var result = await _controller.GetActiveAssignmentsByEmployee(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetActiveAssignmentsByEmployee_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetActiveAssignmentsByEmployeeIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<DiningAreaAssignmentListResponse>>(new Error("Employee.NotFound", "funcionario nao encontrado")));

        var result = await _controller.GetActiveAssignmentsByEmployee(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task StartAssignment_Success_ShouldSendCommandWithAreaIdAndReturnOkWithValue()
    {
        var startAt = DateTime.Today;
        var request = new StartAssignmentRequest(1, startAt);
        _mediator.Send(Arg.Is<CreateDiningAreaAssignmentCommand>(c => c.DiningAreaId == 2 && c.EmployeeId == 1 && c.StartAt == startAt),
            Arg.Any<CancellationToken>()).Returns(Result.Success(10L));

        var result = await _controller.StartAssignment(2, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(10L);
    }

    [Fact]
    public async Task StartAssignment_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new StartAssignmentRequest(1, DateTime.Today);
        _mediator.Send(Arg.Any<CreateDiningAreaAssignmentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("DiningArea.NotFound", "area nao encontrada")));

        var result = await _controller.StartAssignment(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task EndAssignment_Success_ShouldSendCommandWithAssignmentIdAndReturnNoContent()
    {
        var endAt = DateTime.Today;
        var request = new EndAssignmentRequest(endAt);
        _mediator.Send(Arg.Is<EndDiningAreaAssignmentCommand>(c => c.Id == 1 && c.EndAt == endAt), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.EndAssignment(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task EndAssignment_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new EndAssignmentRequest(DateTime.Today);
        _mediator.Send(Arg.Any<EndDiningAreaAssignmentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("DiningAreaAssignment.NotFound", "atribuicao nao encontrada")));

        var result = await _controller.EndAssignment(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMessagesByBranch_Success_ShouldForwardOptionalAreaIdAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetWaiterMessagesByBranchQuery>(q => q.BranchId == 1 && q.DiningAreaId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IEnumerable<WaiterMessageResponse>>([]));

        var result = await _controller.GetMessagesByBranch(1, 2, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMessagesByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetWaiterMessagesByBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IEnumerable<WaiterMessageResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetMessagesByBranch(999, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SendMessage_Success_ShouldReturnOkWithValue()
    {
        var command = new CreateWaiterMessageCommand(1, 1, null, 1, "Mesa 5 precisa de atendimento");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(11L));

        var result = await _controller.SendMessage(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(11L);
    }

    [Fact]
    public async Task SendMessage_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateWaiterMessageCommand(1, 1, null, 1, "");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("WaiterMessage.EmptyMessage", "mensagem obrigatoria")));

        var result = await _controller.SendMessage(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
