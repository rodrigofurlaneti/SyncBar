using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.CreateCategory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.CreateCategory;

public sealed class CreateCategoryCommandHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateCategoryCommandHandler _handler;

    public CreateCategoryCommandHandlerTests()
    {
        _handler = new CreateCategoryCommandHandler(_categoryRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailure()
    {
        var result = await _handler.Handle(new CreateCategoryCommand(1, "", 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.EmptyName");
        await _categoryRepository.DidNotReceive().AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldPersistAndTriggerSync()
    {
        var result = await _handler.Handle(new CreateCategoryCommand(3, "Bebidas", 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _categoryRepository.Received(1).AddAsync(
            Arg.Is<Category>(c => c.Name == "Bebidas" && c.CompanyId == 3), Arg.Any<CancellationToken>());
        _catalogSyncTrigger.Received(1).TriggerCompanySync(3);
    }
}
