using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.Pizza.AddPizzaSize;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Pizza.AddPizzaSize;

public sealed class AddPizzaSizeCommandHandlerTests
{
    private readonly IPizzaConfigurationRepository _pizzaConfigurationRepository = Substitute.For<IPizzaConfigurationRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AddPizzaSizeCommandHandler _handler;

    public AddPizzaSizeCommandHandlerTests()
    {
        _handler = new AddPizzaSizeCommandHandler(_pizzaConfigurationRepository, _productRepository, _logRepository, _unitOfWork);
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

        var result = await _handler.Handle(new AddPizzaSizeCommand(1, "Grande", 8, 1, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        var configuration = CreateConfiguration(productId: 5);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new AddPizzaSizeCommand(configuration.Id, "Grande", 8, 1, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.NotFound");
    }

    [Fact]
    public async Task Handle_DuplicateSizeName_ShouldReturnFailure()
    {
        var configuration = CreateConfiguration(productId: 5);
        configuration.AddSize("Grande", 8, 1, 0);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(CreateProduct(5));

        var result = await _handler.Handle(new AddPizzaSizeCommand(configuration.Id, "grande", 8, 1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaConfiguration.DuplicateSizeName");
    }

    [Fact]
    public async Task Handle_InvalidAcceptedFractions_ShouldReturnFailure()
    {
        var configuration = CreateConfiguration(productId: 5);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(CreateProduct(5));

        var result = await _handler.Handle(new AddPizzaSizeCommand(configuration.Id, "Grande", 8, 0, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PizzaSize.InvalidAcceptedFractions");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldAddSize()
    {
        var configuration = CreateConfiguration(productId: 5);
        _pizzaConfigurationRepository.GetByIdForUpdateAsync(configuration.Id, Arg.Any<CancellationToken>()).Returns(configuration);
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(CreateProduct(5));

        var result = await _handler.Handle(new AddPizzaSizeCommand(configuration.Id, "Grande", 8, 2, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        configuration.Sizes.Should().ContainSingle(s => s.Name == "Grande" && s.AcceptedFractions == 2);
    }
}
