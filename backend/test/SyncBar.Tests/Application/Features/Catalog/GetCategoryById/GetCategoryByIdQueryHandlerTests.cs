using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.GetCategoryById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.GetCategoryById;

public sealed class GetCategoryByIdQueryHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCategoryByIdQueryHandler _handler;

    public GetCategoryByIdQueryHandlerTests()
    {
        _handler = new GetCategoryByIdQueryHandler(_categoryRepository, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    [Fact]
    public async Task Handle_CategoryNotFound_ShouldReturnFailure()
    {
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _handler.Handle(new GetCategoryByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task Handle_CategoryInactive_ShouldReturnFailure()
    {
        var category = Category.Create(1, "Bebidas", 1).Value;
        category.Deactivate();
        SetId(category, 1);
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(new GetCategoryByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task Handle_ValidCategory_ShouldReturnResponse()
    {
        var category = Category.Create(1, "Bebidas", 3).Value;
        SetId(category, 5);
        _categoryRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(new GetCategoryByIdQuery(5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(5);
        result.Value.Name.Should().Be("Bebidas");
        result.Value.DisplayOrder.Should().Be(3);
    }
}
