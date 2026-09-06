using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.UnlinkProductComplementGroup;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.UnlinkProductComplementGroup;

public sealed class UnlinkProductComplementGroupCommandHandlerTests
{
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UnlinkProductComplementGroupCommandHandler _handler;

    public UnlinkProductComplementGroupCommandHandlerTests()
    {
        _handler = new UnlinkProductComplementGroupCommandHandler(
            _productComplementGroupRepository, _productRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static ProductComplementGroup CreateLink(long id = 1, long productId = 10, bool active = true)
    {
        var link = ProductComplementGroup.Create(productId, 1, 0).Value;
        if (!active)
            link.Deactivate();
        SetId(link, id);
        return link;
    }

    private static Product CreateProduct(long id, long companyId)
    {
        var product = Product.Create(companyId, 1, 1, "X-Burguer", null, null, 10m, null, false, null).Value;
        SetId(product, id);
        return product;
    }

    [Fact]
    public async Task Handle_LinkNotFound_ShouldReturnFailure()
    {
        _productComplementGroupRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((ProductComplementGroup?)null);

        var result = await _handler.Handle(new UnlinkProductComplementGroupCommand(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProductComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_AlreadyInactive_ShouldReturnFailure()
    {
        var link = CreateLink(active: false);
        _productComplementGroupRepository.GetByIdForUpdateAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);

        var result = await _handler.Handle(new UnlinkProductComplementGroupCommand(link.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProductComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_ProductFound_ShouldDeactivateAndTriggerSync()
    {
        var link = CreateLink(productId: 10);
        _productComplementGroupRepository.GetByIdForUpdateAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);
        var product = CreateProduct(10, companyId: 5);
        _productRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new UnlinkProductComplementGroupCommand(link.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        link.IsActive.Should().BeFalse();
        _catalogSyncTrigger.Received(1).TriggerCompanySync(5);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldStillSucceedWithoutTrigger()
    {
        var link = CreateLink(productId: 10);
        _productComplementGroupRepository.GetByIdForUpdateAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);
        _productRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new UnlinkProductComplementGroupCommand(link.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        link.IsActive.Should().BeFalse();
        _catalogSyncTrigger.DidNotReceive().TriggerCompanySync(Arg.Any<long>());
    }
}
