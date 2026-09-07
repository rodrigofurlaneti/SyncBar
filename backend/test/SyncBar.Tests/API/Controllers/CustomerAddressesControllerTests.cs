using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.CustomerAddresses.Create;
using SyncBar.Application.Features.CustomerAddresses.GetByBranchId;
using SyncBar.Application.Features.CustomerAddresses.GetByCompanyId;
using SyncBar.Application.Features.CustomerAddresses.GetByCustomerId;
using SyncBar.Application.Features.CustomerAddresses.GetById;
using SyncBar.Application.Features.CustomerAddresses.RegisterOrder;
using SyncBar.Application.Features.CustomerAddresses.Remove;
using SyncBar.Application.Features.CustomerAddresses.Update;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

// CustomerAddressResponse é definido de forma independente (não compartilhada) em cada
// sub-namespace de feature (GetById/GetByCustomerId/GetByCompanyId/GetByBranchId) — mesma
// armadilha já vista em CustomerAppUser: usar mais de um `using` desses juntos deixa o nome
// ambíguo, então os testes abaixo qualificam o tipo por completo em vez de importá-lo.
public sealed class CustomerAddressesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CustomerAddressesController _controller;

    public CustomerAddressesControllerTests()
    {
        _controller = new CustomerAddressesController(_mediator);
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ILogTrackerRepository)).Returns(_logRepository);
        services.GetService(typeof(IUnitOfWork)).Returns(_unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller, services);
    }

    private static CreateCustomerAddressRequest ValidCreateRequest() => new(1, null, null, "Av Paulista", "100", "Apto 1", "01310000");

    [Fact]
    public async Task Create_Success_ShouldReturnOkWithValue()
    {
        var request = ValidCreateRequest();
        _mediator.Send(Arg.Is<CreateCustomerAddressCommand>(c => c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(3L));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(3L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var request = ValidCreateRequest();
        _mediator.Send(Arg.Any<CreateCustomerAddressCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_MissingCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new CreateCustomerAddressRequest(null, null, null, "Av Paulista", "100", "Apto 1", "01310000");

        var result = await _controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<CreateCustomerAddressCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_IdMismatch_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new UpdateCustomerAddressRequest(2, 1, null, null, "Av Paulista", "100", "Apto 1", "01310000");

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateCustomerAddressCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_MissingCompanyId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new UpdateCustomerAddressRequest(1, null, null, null, "Av Paulista", "100", "Apto 1", "01310000");

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<UpdateCustomerAddressCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_Success_ShouldReturnNoContent()
    {
        var request = new UpdateCustomerAddressRequest(1, 1, null, null, "Av Paulista", "100", "Apto 1", "01310000");
        _mediator.Send(Arg.Is<UpdateCustomerAddressCommand>(c => c.Id == 1 && c.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateCustomerAddressRequest(1, 1, null, null, "Av Paulista", "100", "Apto 1", "01310000");
        _mediator.Send(Arg.Any<UpdateCustomerAddressCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerAddress.NotFound", "endereco nao encontrado")));

        var result = await _controller.Update(1, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Remove_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<RemoveCustomerAddressCommand>(c => c.Id == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Remove(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Remove_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RemoveCustomerAddressCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerAddress.NotFound", "endereco nao encontrado")));

        var result = await _controller.Remove(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCustomerAddressByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((SyncBar.Application.Features.CustomerAddresses.GetById.CustomerAddressResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCustomerAddressByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SyncBar.Application.Features.CustomerAddresses.GetById.CustomerAddressResponse>(new Error("CustomerAddress.NotFound", "endereco nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCustomerAddressesByCustomerIdQuery>(q => q.CustomerId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IEnumerable<SyncBar.Application.Features.CustomerAddresses.GetByCustomerId.CustomerAddressResponse>>([]));

        var result = await _controller.GetByCustomerId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCustomerId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCustomerAddressesByCustomerIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IEnumerable<SyncBar.Application.Features.CustomerAddresses.GetByCustomerId.CustomerAddressResponse>>(new Error("Customer.NotFound", "cliente nao encontrado")));

        var result = await _controller.GetByCustomerId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByCompanyId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCustomerAddressesByCompanyIdQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IEnumerable<SyncBar.Application.Features.CustomerAddresses.GetByCompanyId.CustomerAddressResponse>>([]));

        var result = await _controller.GetByCompanyId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCompanyId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCustomerAddressesByCompanyIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IEnumerable<SyncBar.Application.Features.CustomerAddresses.GetByCompanyId.CustomerAddressResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetByCompanyId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetByBranchId_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetCustomerAddressesByBranchIdQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IEnumerable<SyncBar.Application.Features.CustomerAddresses.GetByBranchId.CustomerAddressResponse>>([]));

        var result = await _controller.GetByBranchId(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranchId_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetCustomerAddressesByBranchIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IEnumerable<SyncBar.Application.Features.CustomerAddresses.GetByBranchId.CustomerAddressResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranchId(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RegisterOrder_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new RegisterCustomerAddressOrderRequest(7);
        _mediator.Send(Arg.Is<RegisterCustomerAddressOrderCommand>(c => c.AddressId == 1 && c.OrderId == 7), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.RegisterOrder(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RegisterOrder_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new RegisterCustomerAddressOrderRequest(7);
        _mediator.Send(Arg.Any<RegisterCustomerAddressOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerAddress.NotFound", "endereco nao encontrado")));

        var result = await _controller.RegisterOrder(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RegisterOrder_MissingOrderId_ShouldReturnBadRequestWithoutCallingMediator()
    {
        var request = new RegisterCustomerAddressOrderRequest(null);

        var result = await _controller.RegisterOrder(1, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        await _mediator.DidNotReceive().Send(Arg.Any<RegisterCustomerAddressOrderCommand>(), Arg.Any<CancellationToken>());
    }
}
