using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Create;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Delete;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Exists;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetByAsaasCustomerId;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetById;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class AsaasCustomerControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AsaasCustomerController _controller;

    public AsaasCustomerControllerTests()
    {
        _controller = new AsaasCustomerController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAsaasCustomerByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId.AsaasIntegrationCustomerResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAsaasCustomerByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId.AsaasIntegrationCustomerResponse>(new Error("AsaasIntegrationCustomer.NotFound", "vinculo nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAllByCompany_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAllAsaasCustomersByCompanyIdQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId.AsaasIntegrationCustomerResponse>>([]));

        var result = await _controller.GetAllByCompany(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllByCompany_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAllAsaasCustomersByCompanyIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId.AsaasIntegrationCustomerResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetAllByCompany(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerAndCompany_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetByCustomerIdAndCompanyIdQuery>(q => q.CustomerId == 2 && q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId.AsaasIntegrationCustomerResponse)null!));

        var result = await _controller.GetByCustomerAndCompany(1, 2, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerAndCompany_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetByCustomerIdAndCompanyIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId.AsaasIntegrationCustomerResponse>(
                new Error("AsaasIntegrationCustomer.NotFound", "vinculo nao encontrado")));

        var result = await _controller.GetByCustomerAndCompany(999, 999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByAsaasCustomerId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetByAsaasCustomerIdQuery>(q => q.AsaasCustomerId == "cus_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId.AsaasIntegrationCustomerResponse)null!));

        var result = await _controller.GetByAsaasCustomerId("cus_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByAsaasCustomerId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetByAsaasCustomerIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId.AsaasIntegrationCustomerResponse>(new Error("AsaasIntegrationCustomer.NotFound", "vinculo nao encontrado")));

        var result = await _controller.GetByAsaasCustomerId("cus_missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Exists_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsAsaasCustomerQuery>(q => q.CustomerId == 1 && q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.Exists(1, 1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task Exists_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsAsaasCustomerQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.Exists(0, 0, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var command = new CreateAsaasIntegrationCustomerCommand(1, 1, "cus_1");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.Create(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.RouteValues!["id"].Should().Be(5L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateAsaasIntegrationCustomerCommand(1, 1, "cus_1");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("AsaasIntegrationCustomer.AlreadyExists", "vinculo ja existe")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateCustomerRequest("cus_new");
        _mediator.Send(Arg.Is<UpdateAsaasIntegrationCustomerCommand>(c => c.Id == 1 && c.NewAsaasCustomerId == "cus_new"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateCustomerRequest("cus_new");
        _mediator.Send(Arg.Any<UpdateAsaasIntegrationCustomerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasIntegrationCustomer.NotFound", "vinculo nao encontrado")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithBothIdsAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteAsaasCustomerCommand>(c => c.CompanyId == 1 && c.CustomerId == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, 2, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteAsaasCustomerCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasIntegrationCustomer.NotFound", "vinculo nao encontrado")));

        var result = await _controller.Delete(999, 999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
