using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Catalog.GetCategories;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.GetCategories;

public sealed class GetCategoriesQueryHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCategoriesQueryHandler _handler;

    public GetCategoriesQueryHandlerTests()
    {
        _handler = new GetCategoriesQueryHandler(_categoryRepository, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Category CreateCategory(long id, string name, int displayOrder, long companyId = 1)
    {
        var category = Category.Create(companyId, name, displayOrder).Value;
        SetId(category, id);
        return category;
    }

    [Fact]
    public async Task Handle_ShouldReturnCategoriesOrderedByDisplayOrderThenName()
    {
        var categories = new[]
        {
            CreateCategory(1, "Sobremesas", 2),
            CreateCategory(2, "Bebidas", 1),
            CreateCategory(3, "Águas", 1),
        };
        _categoryRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(categories);

        var result = await _handler.Handle(new GetCategoriesQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(c => c.Name).Should().ContainInOrder("Águas", "Bebidas", "Sobremesas");
    }

    [Fact]
    public async Task Handle_NoCategories_ShouldReturnEmptyCollection()
    {
        _categoryRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<Category>());

        var result = await _handler.Handle(new GetCategoriesQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
