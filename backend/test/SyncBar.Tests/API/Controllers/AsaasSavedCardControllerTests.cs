using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Create;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Delete;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.ExistsByToken;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetById;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByToken;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class AsaasSavedCardControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AsaasSavedCardController _controller;

    public AsaasSavedCardControllerTests()
    {
        _controller = new AsaasSavedCardController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAsaasSavedCardByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((AsaasIntegrationSavedCardResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAsaasSavedCardByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AsaasIntegrationSavedCardResponse>(new Error("AsaasIntegrationSavedCard.NotFound", "cartao nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetSavedCardsByCustomerIdQuery>(q => q.CustomerId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<AsaasIntegrationSavedCardResponse>>([]));

        var result = await _controller.GetByCustomerId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetSavedCardsByCustomerIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<AsaasIntegrationSavedCardResponse>>(new Error("Customer.NotFound", "cliente nao encontrado")));

        var result = await _controller.GetByCustomerId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByToken_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAsaasSavedCardByTokenQuery>(q => q.CreditCardToken == "tok_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((AsaasIntegrationSavedCardResponse)null!));

        var result = await _controller.GetByToken("tok_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByToken_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAsaasSavedCardByTokenQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AsaasIntegrationSavedCardResponse>(new Error("AsaasIntegrationSavedCard.NotFound", "cartao nao encontrado")));

        var result = await _controller.GetByToken("tok_missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ExistsByToken_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsByTokenQuery>(q => q.CreditCardToken == "tok_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.ExistsByToken("tok_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ExistsByToken_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsByTokenQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.ExistsByToken("", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CreateAsaasSavedCardRequest ValidCreateRequest() =>
        new(1, 1, "Joao Silva", "4111111111111111", "12", "2030", "123");

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var request = ValidCreateRequest();
        var response = new CreateAsaasIntegrationSavedCardResponse(5L, 1, 1, "VISA", "1111", false);
        _mediator.Send(Arg.Is<CreateAsaasIntegrationSavedCardCommand>(c => c.CustomerId == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
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
        _mediator.Send(Arg.Any<CreateAsaasIntegrationSavedCardCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CreateAsaasIntegrationSavedCardResponse>(new Error("Customer.NotFound", "cliente nao encontrado")));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_MissingCustomerIdOrCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new CreateAsaasSavedCardRequest(null, 1, "Joao Silva", "4111111111111111", "12", "2030", "123");

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateAsaasIntegrationSavedCardCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateAsaasSavedCardRequest(1, 1, "Joao Silva");
        _mediator.Send(Arg.Is<UpdateAsaasIntegrationSavedCardCommand>(c => c.Id == 1 && c.CustomerId == 1 && c.CompanyId == 1),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateAsaasSavedCardRequest(1, 1);
        _mediator.Send(Arg.Any<UpdateAsaasIntegrationSavedCardCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasIntegrationSavedCard.NotFound", "cartao nao encontrado")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_MissingCustomerIdOrCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new UpdateAsaasSavedCardRequest(1, null);

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateAsaasIntegrationSavedCardCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDefault_Success_ShouldSendCommandWithSetAsDefaultTrueAndReturnNoContent()
    {
        var request = new SetDefaultSavedCardRequest(1, 1);
        _mediator.Send(Arg.Is<UpdateAsaasIntegrationSavedCardCommand>(c => c.Id == 1 && c.SetAsDefault == true), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.SetDefault(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetDefault_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new SetDefaultSavedCardRequest(1, 1);
        _mediator.Send(Arg.Any<UpdateAsaasIntegrationSavedCardCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasIntegrationSavedCard.NotFound", "cartao nao encontrado")));

        var result = await _controller.SetDefault(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetDefault_MissingCustomerIdOrCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new SetDefaultSavedCardRequest(null, 1);

        var result = await _controller.SetDefault(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateAsaasIntegrationSavedCardCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithAllIdsAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteAsaasIntegrationSavedCardCommand>(c => c.Id == 1 && c.CustomerId == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, 1, 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteAsaasIntegrationSavedCardCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasIntegrationSavedCard.NotFound", "cartao nao encontrado")));

        var result = await _controller.Delete(999, 1, 1, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
