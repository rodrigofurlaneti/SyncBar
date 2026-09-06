using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Catalog.Pizza.AddPizzaCrust;
using SyncBar.Application.Features.Catalog.Pizza.AddPizzaEdge;
using SyncBar.Application.Features.Catalog.Pizza.AddPizzaSize;
using SyncBar.Application.Features.Catalog.Pizza.CreatePizzaConfiguration;
using SyncBar.Application.Features.Catalog.Pizza.CreatePizzaFlavor;
using SyncBar.Application.Features.Catalog.Pizza.SetPizzaFlavorPrice;
using SyncBar.Application.Features.Integrations.Ifood.Catalog.Pizza;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class PizzaControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PizzaController _controller;

    public PizzaControllerTests()
    {
        _controller = new PizzaController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task CreateFlavor_Success_ShouldReturnOkWithValue()
    {
        var command = new CreatePizzaFlavorCommand(1, "Calabresa", null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(3L));

        var result = await _controller.CreateFlavor(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(3L);
    }

    [Fact]
    public async Task CreateFlavor_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreatePizzaFlavorCommand(1, "", null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("PizzaFlavor.EmptyName", "nome obrigatorio")));

        var result = await _controller.CreateFlavor(command, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateConfiguration_Success_ShouldReturnOkWithValue()
    {
        var command = new CreatePizzaConfigurationCommand(1);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(4L));

        var result = await _controller.CreateConfiguration(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(4L);
    }

    [Fact]
    public async Task CreateConfiguration_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new CreatePizzaConfigurationCommand(999);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.CreateConfiguration(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddSize_Success_ShouldSendCommandWithIdAndReturnOkWithValue()
    {
        var request = new AddPizzaSizeRequest("Grande", 8, 4, 0);
        _mediator.Send(Arg.Is<AddPizzaSizeCommand>(c => c.PizzaConfigurationId == 1 && c.Name == "Grande" && c.Slices == 8 && c.AcceptedFractions == 4),
            Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.AddSize(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(5L);
    }

    [Fact]
    public async Task AddSize_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AddPizzaSizeRequest("Grande", null, 1, 0);
        _mediator.Send(Arg.Any<AddPizzaSizeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("PizzaConfiguration.NotFound", "configuracao nao encontrada")));

        var result = await _controller.AddSize(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddCrust_Success_ShouldSendCommandWithIdAndReturnOkWithValue()
    {
        var request = new AddPizzaCrustRequest("Catupiry", 5m, 0);
        _mediator.Send(Arg.Is<AddPizzaCrustCommand>(c => c.PizzaConfigurationId == 1 && c.Name == "Catupiry" && c.ExtraPrice == 5m),
            Arg.Any<CancellationToken>()).Returns(Result.Success(6L));

        var result = await _controller.AddCrust(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(6L);
    }

    [Fact]
    public async Task AddCrust_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AddPizzaCrustRequest("Catupiry", 5m, 0);
        _mediator.Send(Arg.Any<AddPizzaCrustCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("PizzaConfiguration.NotFound", "configuracao nao encontrada")));

        var result = await _controller.AddCrust(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddEdge_Success_ShouldSendCommandWithIdAndReturnOkWithValue()
    {
        var request = new AddPizzaEdgeRequest("Cheddar", 3m, 0);
        _mediator.Send(Arg.Is<AddPizzaEdgeCommand>(c => c.PizzaConfigurationId == 1 && c.Name == "Cheddar" && c.ExtraPrice == 3m),
            Arg.Any<CancellationToken>()).Returns(Result.Success(7L));

        var result = await _controller.AddEdge(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(7L);
    }

    [Fact]
    public async Task AddEdge_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AddPizzaEdgeRequest("Cheddar", 3m, 0);
        _mediator.Send(Arg.Any<AddPizzaEdgeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("PizzaConfiguration.NotFound", "configuracao nao encontrada")));

        var result = await _controller.AddEdge(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetFlavorPrice_Success_ShouldSendCommandWithIdAndReturnOkWithValue()
    {
        var request = new SetPizzaFlavorPriceRequest(1, 2, 45m);
        _mediator.Send(Arg.Is<SetPizzaFlavorPriceCommand>(c => c.PizzaConfigurationId == 1 && c.PizzaFlavorId == 1 && c.PizzaSizeId == 2 && c.Price == 45m),
            Arg.Any<CancellationToken>()).Returns(Result.Success(8L));

        var result = await _controller.SetFlavorPrice(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(8L);
    }

    [Fact]
    public async Task SetFlavorPrice_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new SetPizzaFlavorPriceRequest(1, 2, 45m);
        _mediator.Send(Arg.Any<SetPizzaFlavorPriceCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("PizzaConfiguration.SizeNotFound", "tamanho nao encontrado")));

        var result = await _controller.SetFlavorPrice(999, request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SyncWithIfood_Success_ShouldSendCommandWithBranchAndConfigurationIdsAndReturnOkWithValue()
    {
        var request = new SyncIfoodPizzaRequest(2);
        var response = new SyncIfoodPizzaResult("ifood-pizza-1");
        _mediator.Send(Arg.Is<SyncIfoodPizzaCommand>(c => c.BranchId == 2 && c.PizzaConfigurationId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.SyncWithIfood(1, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task SyncWithIfood_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new SyncIfoodPizzaRequest(2);
        _mediator.Send(Arg.Any<SyncIfoodPizzaCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<SyncIfoodPizzaResult>(new Error("PizzaConfiguration.NotFound", "configuracao nao encontrada")));

        var result = await _controller.SyncWithIfood(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
