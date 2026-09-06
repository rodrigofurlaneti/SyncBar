using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Delete;
using SyncBar.Application.Features.Integrations.Asaas.Payment.ExistsByAsaasPaymentId;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByBranchId;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByCustomerOrderId;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetById;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetPendingByBranchId;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class AsaasPaymentControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AsaasPaymentController _controller;

    public AsaasPaymentControllerTests()
    {
        _controller = new AsaasPaymentController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetAsaasPaymentByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((AsaasIntegrationPaymentResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetAsaasPaymentByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AsaasIntegrationPaymentResponse>(new Error("AsaasIntegrationPayment.NotFound", "pagamento nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByAsaasPaymentId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetByAsaasPaymentIdQuery>(q => q.AsaasPaymentId == "pay_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((AsaasIntegrationPaymentResponse)null!));

        var result = await _controller.GetByAsaasPaymentId("pay_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByAsaasPaymentId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetByAsaasPaymentIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AsaasIntegrationPaymentResponse>(new Error("AsaasIntegrationPayment.NotFound", "pagamento nao encontrado")));

        var result = await _controller.GetByAsaasPaymentId("pay_missing", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerOrderId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetByCustomerOrderIdQuery>(q => q.CustomerOrderId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((AsaasIntegrationPaymentResponse)null!));

        var result = await _controller.GetByCustomerOrderId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerOrderId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetByCustomerOrderIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AsaasIntegrationPaymentResponse>(new Error("AsaasIntegrationPayment.NotFound", "pagamento nao encontrado")));

        var result = await _controller.GetByCustomerOrderId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByBranchId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetByBranchIdQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<AsaasIntegrationPaymentResponse>>([]));

        var result = await _controller.GetByBranchId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranchId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetByBranchIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<AsaasIntegrationPaymentResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranchId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPendingByBranchId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetPendingAsaasPaymentsByBranchIdQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<AsaasIntegrationPaymentResponse>>([]));

        var result = await _controller.GetPendingByBranchId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPendingByBranchId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPendingAsaasPaymentsByBranchIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<AsaasIntegrationPaymentResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetPendingByBranchId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Exists_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<ExistsByAsaasPaymentIdQuery>(q => q.AsaasPaymentId == "pay_1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));

        var result = await _controller.Exists("pay_1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task Exists_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ExistsByAsaasPaymentIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(new Error("Generic.Invalid", "parametros invalidos")));

        var result = await _controller.Exists("", CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static CreateAsaasIntegrationPaymentCommand ValidCreateCommand() =>
        new(1, 1, null, "PIX", 100m, DateTime.Today.AddDays(3));

    [Fact]
    public async Task Create_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var command = ValidCreateCommand();
        var response = new CreateAsaasIntegrationPaymentResponse(5L, "pay_1", "PENDING", null, null, null, null);
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
            .Returns(Result.Failure<CreateAsaasIntegrationPaymentResponse>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new UpdateAsaasPaymentRequest("RECEIVED", 95m, DateTime.Today);
        _mediator.Send(Arg.Is<UpdateAsaasIntegrationPaymentCommand>(c => c.Id == 1 && c.Status == "RECEIVED"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateAsaasPaymentRequest("RECEIVED");
        _mediator.Send(Arg.Any<UpdateAsaasIntegrationPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasIntegrationPayment.NotFound", "pagamento nao encontrado")));

        var result = await _controller.Update(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeleteAsaasPaymentCommand>(c => c.PaymentId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeleteAsaasPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("AsaasIntegrationPayment.NotFound", "pagamento nao encontrado")));

        var result = await _controller.Delete(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
