using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Suppliers;
using SyncBar.Application.Features.Suppliers.Create;
using SyncBar.Application.Features.Suppliers.Deactivate;
using SyncBar.Application.Features.Suppliers.GetByCompany;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class SuppliersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SuppliersController _controller;

    public SuppliersControllerTests()
    {
        _controller = new SuppliersController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByCompany_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetSuppliersByCompanyQuery>(q => q.CompanyId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<SupplierResponse>>([]));

        var result = await _controller.GetByCompany(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCompany_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetSuppliersByCompanyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<SupplierResponse>>(new Error("Company.NotFound", "empresa nao encontrada")));

        var result = await _controller.GetByCompany(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_Success_ShouldReturnOkWithValue()
    {
        var command = new CreateSupplierCommand(1, "Distribuidora XYZ", null, null, null, null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(7L));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(7L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreateSupplierCommand(1, "", null, null, null, null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Supplier.EmptyName", "nome obrigatorio")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Deactivate_Success_ShouldReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeactivateSupplierCommand>(c => c.SupplierId == 1), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Deactivate(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeactivateSupplierCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Supplier.NotFound", "fornecedor nao encontrado")));

        var result = await _controller.Deactivate(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
