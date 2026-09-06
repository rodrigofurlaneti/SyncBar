using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.Pizza.AddPizzaCrust;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Pizza.AddPizzaCrust;

public sealed class AddPizzaCrustCommandHandlerTests
{
    private readonly IPizzaConfigurationRepository _pizzaConfigurationRepository = Substitute.For<IPizzaConfigurationRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AddPizzaCrustCommandHandler _handler;

    public AddPizzaCrustCommandHandlerTests()
    {
        _handler = new AddPizzaCrustCommandHandler(_pizzaConfigurationRepository, _productRepository, _logRepository, _unitOfWork);
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

        var result = await _handler.Handle(new AddPizzaCrustCommand(1, "Borda Recheada", 8m, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_ConfigurationInactive_ShouldReturnFailure()
    {
        var configuration = CreateConfiguration(active: false);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);

        var result = await _handler.Handle(new AddPizzaCrustCommand(configuration.Id, "Borda Recheada", 8m, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        var configuration = CreateConfiguration(productId: 5);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new AddPizzaCrustCommand(configuration.Id, "Borda Recheada", 8m, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailure()
    {
        var configuration = CreateConfiguration(productId: 5);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(CreateProduct(5));

        var result = await _handler.Handle(new AddPizzaCrustCommand(configuration.Id, "", 8m, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaCrust.EmptyName");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldAddCrust()
    {
        var configuration = CreateConfiguration(productId: 5);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(CreateProduct(5));

        var result = await _handler.Handle(new AddPizzaCrustCommand(configuration.Id, "Borda Recheada", 8m, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        configuration.Crusts.Should().ContainSingle(c => c.Name == "Borda Recheada" && c.ExtraPrice == 8m);
    }
}
