using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Keeta.Setting.Create;
using SyncBar.Application.Features.Integrations.Keeta.Setting.Delete;
using SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForBranch;
using SyncBar.Application.Features.Integrations.Keeta.Setting.ExistsForCompany;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchId;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchOrCompanyFallback;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByCompanyId;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetById;
using SyncBar.Application.Features.Integrations.Keeta.Setting.GetByScope;
using SyncBar.Application.Features.Integrations.Keeta.Setting.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class KeetaSettingControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly KeetaSettingController _controller;

    public KeetaSettingControllerTests()
    {
        _controller = new KeetaSettingController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaSettingByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationSettingResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaSettingByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationSettingResponse>(new Error("KeetaIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByCompanyId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaSettingByCompanyIdQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationSettingResponse)null!));

        var result = await _controller.GetByCompanyId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCompanyId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaSettingByCompanyIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationSettingResponse>(new Error("KeetaIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.GetByCompanyId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByBranchId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaSettingByBranchIdQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationSettingResponse)null!));

        var result = await _controller.GetByBranchId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranchId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaSettingByBranchIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationSettingResponse>(new Error("KeetaIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.GetByBranchId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ResolveActiveSetting_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaSettingByBranchOrCompanyFallbackQuery>(q => q.CompanyId == 1 && q.BranchId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationSettingResponse)null!));

        var result = await _controller.ResolveActiveSetting(1, 2, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResolveActiveSetting_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaSettingByBranchOrCompanyFallbackQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationSettingResponse>(new Error("KeetaIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.ResolveActiveSetting(999, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByScope_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaSettingByScopeQuery>(q => q.CompanyId == 1 && q.BranchId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationSettingResponse)null!));

        var result = await _controller.GetByScope(1, 2, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByScope_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaSettingByScopeQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationSettingResponse>(new Error("KeetaIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.GetByScope(999, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ExistsForCompany_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsKeetaSettingForCompanyQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ExistsForCompany(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ExistsForCompany_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsKeetaSettingForCompanyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.ExistsForCompany(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ExistsForBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsKeetaSettingForBranchQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ExistsForBranch(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ExistsForBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsKeetaSettingForBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.ExistsForBranch(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CreateKeetaSettingRequest ValidCreateRequest() => new(1, 1, "client-id", "client-secret", "app-id");

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var request = ValidCreateRequest();
        var response = new CreateKeetaIntegrationSettingResponse(5L, 1, 1, "https://api.keeta.com");
        _mediator.Send(Arg.Is<CreateKeetaIntegrationSettingCommand>(c => c.CompanyId == 1 && c.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.Create(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.RouteValues!["id"].Should().Be(5L);
        created.Value.Should().Be(response);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var request = ValidCreateRequest();
        _mediator.Send(Arg.Any<CreateKeetaIntegrationSettingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CreateKeetaIntegrationSettingResponse>(new Error("KeetaIntegrationSetting.AlreadyExists", "configuracao ja existe")));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_MissingRequiredFields_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new CreateKeetaSettingRequest(1, null, "client-id", "client-secret", "app-id");

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateKeetaIntegrationSettingCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateKeetaSettingRequest(1, "new-client-id");
        _mediator.Send(Arg.Is<UpdateKeetaIntegrationSettingCommand>(c => c.Id == 1 && c.CompanyId == 1 && c.ClientId == "new-client-id"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateKeetaSettingRequest(1);
        _mediator.Send(Arg.Any<UpdateKeetaIntegrationSettingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_MissingCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new UpdateKeetaSettingRequest(null);

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateKeetaIntegrationSettingCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteKeetaIntegrationSettingCommand>(c => c.Id == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteKeetaIntegrationSettingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.Delete(999, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
