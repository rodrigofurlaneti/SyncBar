using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Create;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Delete;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetByAuthId;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetById;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetPendingSessions;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class KeetaAuthorizationSessionControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly KeetaAuthorizationSessionController _controller;

    public KeetaAuthorizationSessionControllerTests()
    {
        _controller = new KeetaAuthorizationSessionController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaAuthorizationSessionByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationAuthorizationSessionResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaAuthorizationSessionByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationAuthorizationSessionResponse>(new Error("KeetaIntegrationAuthorizationSession.NotFound", "sessao nao encontrada")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByAuthId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaAuthorizationSessionByAuthIdQuery>(q => q.AuthId == "auth_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationAuthorizationSessionResponse)null!));

        var result = await _controller.GetByAuthId("auth_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByAuthId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaAuthorizationSessionByAuthIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationAuthorizationSessionResponse>(new Error("KeetaIntegrationAuthorizationSession.NotFound", "sessao nao encontrada")));

        var result = await _controller.GetByAuthId("auth_missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPendingSessions_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Any<GetPendingKeetaAuthorizationSessionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<KeetaIntegrationAuthorizationSessionResponse>>([]));

        var result = await _controller.GetPendingSessions(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPendingSessions_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPendingKeetaAuthorizationSessionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<KeetaIntegrationAuthorizationSessionResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetPendingSessions(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CreateKeetaIntegrationAuthorizationSessionCommand ValidCreateCommand() => new(1, 1, "auth_1", 1);

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var command = ValidCreateCommand();
        var response = new CreateKeetaIntegrationAuthorizationSessionResponse(5L, 1, 1, "auth_1", 1);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.Create(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.RouteValues!["id"].Should().Be(5L);
        created.Value.Should().Be(response);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCreateCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CreateKeetaIntegrationAuthorizationSessionResponse>(new Error("KeetaIntegrationAuthorizationSession.AlreadyExists", "sessao ja existe")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateKeetaAuthorizationSessionRequest(1, 10, "code123", "APPROVED", true);
        _mediator.Send(Arg.Is<UpdateKeetaIntegrationAuthorizationSessionCommand>(c => c.Id == 1 && c.CompanyId == 1 && c.MarkAsProcessed),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateKeetaAuthorizationSessionRequest(1);
        _mediator.Send(Arg.Any<UpdateKeetaIntegrationAuthorizationSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationAuthorizationSession.NotFound", "sessao nao encontrada")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteKeetaIntegrationAuthorizationSessionCommand>(c => c.Id == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteKeetaIntegrationAuthorizationSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationAuthorizationSession.NotFound", "sessao nao encontrada")));

        var result = await _controller.Delete(999, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
