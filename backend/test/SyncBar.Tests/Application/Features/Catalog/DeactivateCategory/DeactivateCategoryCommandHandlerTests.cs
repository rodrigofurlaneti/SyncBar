using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.DeactivateCategory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.DeactivateCategory;

public sealed class DeactivateCategoryCommandHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeactivateCategoryCommandHandler _handler;

    public DeactivateCategoryCommandHandlerTests()
    {
        _handler = new DeactivateCategoryCommandHandler(_categoryRepository, _productRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Category CreateCategory(long id = 1, long companyId = 1, bool active = true)
    {
        var category = Category.Create(companyId, "Bebidas", 1).Value;
        if (!active)
            category.Deactivate();
        SetId(category, id);
        return category;
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ShouldReturnFailure()
    {
        _categoryRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _handler.Handle(new DeactivateCategoryCommand(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }

    [Fact]
    public async Task Handle_AlreadyInactive_ShouldReturnFailure()
    {
        var category = CreateCategory(active: false);
        _categoryRepository.GetByIdForUpdateAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(new DeactivateCategoryCommand(category.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }

    [Fact]
    public async Task Handle_ActiveWithNoLinkedProducts_ShouldDeactivateAndTriggerSync()
    {
        var category = CreateCategory(companyId: 5, active: true);
        _categoryRepository.GetByIdForUpdateAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _productRepository.ExistsActiveByCategoryAsync(category.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new DeactivateCategoryCommand(category.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        category.IsActive.Should().BeFalse();
        _catalogSyncTrigger.Received(1).TriggerCompanySync(5);
    }

    [Fact]
    public async Task Handle_HasActiveLinkedProducts_ShouldReturnFailureAndNotDeactivate()
    {
        var category = CreateCategory(active: true);
        _categoryRepository.GetByIdForUpdateAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _productRepository.ExistsActiveByCategoryAsync(category.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new DeactivateCategoryCommand(category.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.HasLinkedProducts");
        category.IsActive.Should().BeTrue();
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }
}
