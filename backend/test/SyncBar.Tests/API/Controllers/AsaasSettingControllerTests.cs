using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Create;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Delete;
using SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForBranch;
using SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForCompany;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchId;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByBranchOrCompanyFallback;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetByCompanyId;
using SyncBar.Application.Features.Integrations.Asaas.Setting.GetById;
using SyncBar.Application.Features.Integrations.Asaas.Setting.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class AsaasSettingControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AsaasSettingController _controller;

    public AsaasSettingControllerTests()
    {
        _controller = new AsaasSettingController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAsaasSettingByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((AsaasIntegrationSettingResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAsaasSettingByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AsaasIntegrationSettingResponse>(new Error("AsaasIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByCompanyId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAsaasSettingByCompanyIdQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((AsaasIntegrationSettingResponse)null!));

        var result = await _controller.GetByCompanyId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCompanyId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAsaasSettingByCompanyIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AsaasIntegrationSettingResponse>(new Error("AsaasIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.GetByCompanyId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByBranchId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAsaasSettingByBranchIdQuery>(q => q.CompanyId == 1 && q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((AsaasIntegrationSettingResponse)null!));

        var result = await _controller.GetByBranchId(1, 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranchId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAsaasSettingByBranchIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AsaasIntegrationSettingResponse>(new Error("AsaasIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.GetByBranchId(1, 999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ResolveActiveSetting_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetByBranchOrCompanyFallbackQuery>(q => q.CompanyId == 1 && q.BranchId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success((AsaasIntegrationSettingResponse)null!));

        var result = await _controller.ResolveActiveSetting(1, 2, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResolveActiveSetting_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetByBranchOrCompanyFallbackQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AsaasIntegrationSettingResponse>(new Error("AsaasIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.ResolveActiveSetting(999, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAllActive_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAllActiveAsaasSettingsQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<AsaasIntegrationSettingResponse>>([]));

        var result = await _controller.GetAllActive(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllActive_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAllActiveAsaasSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<AsaasIntegrationSettingResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetAllActive(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ExistsForCompany_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsAsaasSettingForCompanyQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ExistsForCompany(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ExistsForCompany_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsAsaasSettingForCompanyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.ExistsForCompany(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ExistsForBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsAsaasSettingForBranchQuery>(q => q.CompanyId == 1 && q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ExistsForBranch(1, 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ExistsForBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsAsaasSettingForBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.ExistsForBranch(999, 999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CreateAsaasSettingRequest ValidCreateRequest() =>
        new(1, null, "api-key-123", "webhook-token", "sandbox");

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var request = ValidCreateRequest();
        var response = new CreateAsaasIntegrationSettingResponse(5L, 1, null, "sandbox", true);
        _mediator.Send(Arg.Is<CreateAsaasIntegrationSettingCommand>(c => c.CompanyId == 1), Arg.Any<CancellationToken>())
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
        _mediator.Send(Arg.Any<CreateAsaasIntegrationSettingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CreateAsaasIntegrationSettingResponse>(new Error("AsaasIntegrationSetting.AlreadyExists", "configuracao ja existe")));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_MissingCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new CreateAsaasSettingRequest(null, null, "api-key-123", "webhook-token", "sandbox");

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateAsaasIntegrationSettingCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateAsaasSettingRequest(1, "new-api-key");
        _mediator.Send(Arg.Is<UpdateAsaasIntegrationSettingCommand>(c => c.Id == 1 && c.CompanyId == 1 && c.ApiKey == "new-api-key"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateAsaasSettingRequest(1);
        _mediator.Send(Arg.Any<UpdateAsaasIntegrationSettingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_MissingCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new UpdateAsaasSettingRequest(null);

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateAsaasIntegrationSettingCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteAsaasIntegrationSettingCommand>(c => c.Id == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteAsaasIntegrationSettingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasIntegrationSetting.NotFound", "configuracao nao encontrada")));

        var result = await _controller.Delete(999, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
