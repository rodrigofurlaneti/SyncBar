using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.DeactivateProduct;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.DeactivateProduct;

public sealed class DeactivateProductCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeactivateProductCommandHandler _handler;

    public DeactivateProductCommandHandlerTests()
    {
        _handler = new DeactivateProductCommandHandler(_productRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Product CreateProduct(long id = 1, long companyId = 1, bool active = true)
    {
        var product = Product.Create(companyId, 1, 1, "X-Burguer", null, null, 25m, 10m, false, null).Value;
        if (!active)
            product.Deactivate();
        SetId(product, id);
        return product;
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        _productRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new DeactivateProductCommand(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }

    [Fact]
    public async Task Handle_AlreadyInactive_ShouldReturnFailure()
    {
        var product = CreateProduct(active: false);
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new DeactivateProductCommand(product.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }

    [Fact]
    public async Task Handle_Active_ShouldDeactivateAndTriggerSync()
    {
        var product = CreateProduct(companyId: 5, active: true);
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new DeactivateProductCommand(product.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeFalse();
        _catalogSyncTrigger.Received(1).TriggerCompanySync(5);
    }
}
