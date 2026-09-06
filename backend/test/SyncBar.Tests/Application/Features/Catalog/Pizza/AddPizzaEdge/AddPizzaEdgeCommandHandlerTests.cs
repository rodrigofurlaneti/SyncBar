using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.Pizza.AddPizzaEdge;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Pizza.AddPizzaEdge;

public sealed class AddPizzaEdgeCommandHandlerTests
{
    private readonly IPizzaConfigurationRepository _pizzaConfigurationRepository = Substitute.For<IPizzaConfigurationRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AddPizzaEdgeCommandHandler _handler;

    public AddPizzaEdgeCommandHandlerTests()
    {
        _handler = new AddPizzaEdgeCommandHandler(_pizzaConfigurationRepository, _productRepository, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static PizzaConfiguration CreateConfiguration(long id = 1, long productId = 1, bool active = true)
    {
        var configuration = PizzaConfiguration.Create(productId).Value;
        if (!active)
            configuration.Deactivate();
        SetId(configuration, id);
        return configuration;
    }

    private static Product CreateProduct(long id)
    {
        var product = Product.Create(1, 1, 1, "Pizza Grande", null, null, 0m, null, false, null).Value;
        SetId(product, id);
        return product;
    }

    [Fact]
    public async Task Handle_ConfigurationNotFound_ShouldReturnFailure()
    {
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((PizzaConfiguration?)null);

        var result = await _handler.Handle(new AddPizzaEdgeCommand(1, "Catupiry", 6m, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_ConfigurationInactive_ShouldReturnFailure()
    {
        var configuration = CreateConfiguration(active: false);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);

        var result = await _handler.Handle(new AddPizzaEdgeCommand(configuration.Id, "Catupiry", 6m, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        var configuration = CreateConfiguration(productId: 5);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new AddPizzaEdgeCommand(configuration.Id, "Catupiry", 6m, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailure()
    {
        var configuration = CreateConfiguration(productId: 5);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(CreateProduct(5));

        var result = await _handler.Handle(new AddPizzaEdgeCommand(configuration.Id, "", 6m, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaEdge.EmptyName");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldAddEdge()
    {
        var configuration = CreateConfiguration(productId: 5);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(CreateProduct(5));

        var result = await _handler.Handle(new AddPizzaEdgeCommand(configuration.Id, "Catupiry", 6m, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        configuration.Edges.Should().ContainSingle(e => e.Name == "Catupiry" && e.ExtraPrice == 6m);
    }
}
