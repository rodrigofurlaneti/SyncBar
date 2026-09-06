using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.ActivateCategory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.ActivateCategory;

public sealed class ActivateCategoryCommandHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ActivateCategoryCommandHandler _handler;

    public ActivateCategoryCommandHandlerTests()
    {
        _handler = new ActivateCategoryCommandHandler(_categoryRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Category CreateInactiveCategory(long id = 1, long companyId = 1)
    {
        var category = Category.Create(companyId, "Bebidas", 1).Value;
        category.Deactivate();
        SetId(category, id);
        return category;
    }

    private static Category CreateActiveCategory(long id = 1, long companyId = 1)
    {
        var category = Category.Create(companyId, "Bebidas", 1).Value;
        SetId(category, id);
        return category;
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ShouldReturnFailure()
    {
        _categoryRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _handler.Handle(new ActivateCategoryCommand(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }

    [Fact]
    public async Task Handle_AlreadyActive_ShouldReturnSuccessWithoutTrigger()
    {
        var category = CreateActiveCategory();
        _categoryRepository.GetByIdForUpdateAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(new ActivateCategoryCommand(category.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }

    [Fact]
    public async Task Handle_Inactive_ShouldActivateCommitAndTriggerSync()
    {
        var category = CreateInactiveCategory(companyId: 7);
        _categoryRepository.GetByIdForUpdateAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(new ActivateCategoryCommand(category.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        category.IsActive.Should().BeTrue();
        await _unitOfWork.Received().CommitAsync(Arg.Any<CancellationToken>());
        _catalogSyncTrigger.Received(1).TriggerCompanySync(7);
    }
}
