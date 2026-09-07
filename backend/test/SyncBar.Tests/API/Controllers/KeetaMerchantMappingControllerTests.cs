using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Create;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Delete;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.ExistsByKeetaMerchantId;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByBranchId;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByCompanyId;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetByInternalMerchantId;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetByKeetaMerchantId;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class KeetaMerchantMappingControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly KeetaMerchantMappingController _controller;

    public KeetaMerchantMappingControllerTests()
    {
        _controller = new KeetaMerchantMappingController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaMerchantMappingByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationMerchantMappingResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaMerchantMappingByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationMerchantMappingResponse>(new Error("KeetaIntegrationMerchantMapping.NotFound", "vinculo nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByKeetaMerchantId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaMerchantMappingByKeetaMerchantIdQuery>(q => q.KeetaMerchantId == 10), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationMerchantMappingResponse)null!));

        var result = await _controller.GetByKeetaMerchantId(10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByKeetaMerchantId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaMerchantMappingByKeetaMerchantIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationMerchantMappingResponse>(new Error("KeetaIntegrationMerchantMapping.NotFound", "vinculo nao encontrado")));

        var result = await _controller.GetByKeetaMerchantId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByInternalMerchantId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetKeetaMerchantMappingByInternalMerchantIdQuery>(q => q.InternalMerchantId == "int_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((KeetaIntegrationMerchantMappingResponse)null!));

        var result = await _controller.GetByInternalMerchantId("int_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByInternalMerchantId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetKeetaMerchantMappingByInternalMerchantIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<KeetaIntegrationMerchantMappingResponse>(new Error("KeetaIntegrationMerchantMapping.NotFound", "vinculo nao encontrado")));

        var result = await _controller.GetByInternalMerchantId("int_missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAllByCompanyId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAllKeetaMerchantMappingsByCompanyIdQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<KeetaIntegrationMerchantMappingResponse>>([]));

        var result = await _controller.GetAllByCompanyId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllByCompanyId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAllKeetaMerchantMappingsByCompanyIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<KeetaIntegrationMerchantMappingResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetAllByCompanyId(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAllByBranchId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAllKeetaMerchantMappingsByBranchIdQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<KeetaIntegrationMerchantMappingResponse>>([]));

        var result = await _controller.GetAllByBranchId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllByBranchId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAllKeetaMerchantMappingsByBranchIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<KeetaIntegrationMerchantMappingResponse>>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.GetAllByBranchId(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ExistsByKeetaMerchantId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsKeetaMerchantMappingByKeetaMerchantIdQuery>(q => q.KeetaMerchantId == 10), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ExistsByKeetaMerchantId(10, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ExistsByKeetaMerchantId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsKeetaMerchantMappingByKeetaMerchantIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.ExistsByKeetaMerchantId(999, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CreateKeetaMerchantMappingRequest ValidCreateRequest() => new(1, 1, "int_1", 10, "Loja Centro");

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var request = ValidCreateRequest();
        var response = new CreateKeetaIntegrationMerchantMappingResponse(5L, 1, 1, "int_1", 10, "Loja Centro");
        _mediator.Send(Arg.Is<CreateKeetaIntegrationMerchantMappingCommand>(c => c.CompanyId == 1 && c.BranchId == 1 && c.KeetaMerchantId == 10), Arg.Any<CancellationToken>())
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
        _mediator.Send(Arg.Any<CreateKeetaIntegrationMerchantMappingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CreateKeetaIntegrationMerchantMappingResponse>(new Error("KeetaIntegrationMerchantMapping.AlreadyExists", "vinculo ja existe")));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_MissingRequiredFields_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new CreateKeetaMerchantMappingRequest(1, 1, "int_1", null, "Loja Centro");

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateKeetaIntegrationMerchantMappingCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateKeetaMerchantMappingRequest(1, true, true, "https://menu", "https://webhook", true);
        _mediator.Send(Arg.Is<UpdateKeetaIntegrationMerchantMappingCommand>(c => c.Id == 1 && c.CompanyId == 1 && c.IsAuthorized == true),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateKeetaMerchantMappingRequest(1);
        _mediator.Send(Arg.Any<UpdateKeetaIntegrationMerchantMappingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationMerchantMapping.NotFound", "vinculo nao encontrado")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_MissingCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new UpdateKeetaMerchantMappingRequest(null);

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateKeetaIntegrationMerchantMappingCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteKeetaIntegrationMerchantMappingCommand>(c => c.Id == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteKeetaIntegrationMerchantMappingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("KeetaIntegrationMerchantMapping.NotFound", "vinculo nao encontrado")));

        var result = await _controller.Delete(999, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
