using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.CreateProduct;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.CreateProduct;

public sealed class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _handler = new CreateProductCommandHandler(
            _productRepository, _categoryRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Category CreateCategoryFixture(long id, long companyId, bool active = true)
    {
        var category = Category.Create(companyId, "Bebidas", 1).Value;
        if (!active)
            category.Deactivate();
        SetId(category, id);
        return category;
    }

    private static CreateProductCommand ValidCommand(long companyId = 1, long categoryId = 1) =>
        new(companyId, categoryId, 1, "X-Burguer", "Descrição", "789123", 25m, 10m, false, 10);

    [Fact]
    public async Task Handle_CategoryNotFound_ShouldReturnFailure()
    {
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task Handle_CategoryInactive_ShouldReturnFailure()
    {
        var category = CreateCategoryFixture(1, 1, active: false);
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(ValidCommand(companyId: 1, categoryId: 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task Handle_CategoryFromDifferentCompany_ShouldReturnFailure()
    {
        var category = CreateCategoryFixture(1, companyId: 99);
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(ValidCommand(companyId: 1, categoryId: 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailure()
    {
        var category = CreateCategoryFixture(1, 1);
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var command = ValidCommand() with { Name = "" };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.EmptyName");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldPersistAndTriggerSync()
    {
        var category = CreateCategoryFixture(1, 3);
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(ValidCommand(companyId: 3, categoryId: 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _productRepository.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Name == "X-Burguer" && p.CompanyId == 3), Arg.Any<CancellationToken>());
        _catalogSyncTrigger.Received(1).TriggerCompanySync(3);
    }
}
