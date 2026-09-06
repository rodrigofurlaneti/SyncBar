using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.ActivateProduct;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.ActivateProduct;

public sealed class ActivateProductCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ActivateProductCommandHandler _handler;

    public ActivateProductCommandHandlerTests()
    {
        _handler = new ActivateProductCommandHandler(_productRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
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

        var result = await _handler.Handle(new ActivateProductCommand(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }

    [Fact]
    public async Task Handle_AlreadyActive_ShouldReturnSuccessWithoutTrigger()
    {
        var product = CreateProduct(active: true);
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new ActivateProductCommand(product.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }

    [Fact]
    public async Task Handle_Inactive_ShouldActivateAndTriggerSync()
    {
        var product = CreateProduct(companyId: 9, active: false);
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new ActivateProductCommand(product.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.IsActive.Should().BeTrue();
        _catalogSyncTrigger.Received(1).TriggerCompanySync(9);
    }
}
