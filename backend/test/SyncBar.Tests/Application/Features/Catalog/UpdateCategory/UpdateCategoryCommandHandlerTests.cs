using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.UpdateCategory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.UpdateCategory;

public sealed class UpdateCategoryCommandHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateCategoryCommandHandler _handler;

    public UpdateCategoryCommandHandlerTests()
    {
        _handler = new UpdateCategoryCommandHandler(_categoryRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Category CreateCategory(long id = 1, long companyId = 3, bool active = true)
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

        var result = await _handler.Handle(new UpdateCategoryCommand(1, "Bebidas", 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task Handle_CategoryInactive_ShouldReturnFailure()
    {
        var category = CreateCategory(active: false);
        _categoryRepository.GetByIdForUpdateAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(new UpdateCategoryCommand(category.Id, "Bebidas", 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailure()
    {
        var category = CreateCategory();
        _categoryRepository.GetByIdForUpdateAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(new UpdateCategoryCommand(category.Id, "", 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.EmptyName");
    }

    [Fact]
    public async Task Handle_NegativeDisplayOrder_ShouldReturnFailure()
    {
        var category = CreateCategory();
        _categoryRepository.GetByIdForUpdateAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(new UpdateCategoryCommand(category.Id, "Bebidas", -1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.InvalidDisplayOrder");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdateAndTriggerSync()
    {
        var category = CreateCategory(companyId: 4);
        _categoryRepository.GetByIdForUpdateAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(new UpdateCategoryCommand(category.Id, "Refrigerantes", 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Refrigerantes");
        category.DisplayOrder.Should().Be(2);
        _catalogSyncTrigger.Received(1).TriggerCompanySync(4);
    }
}
