using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.LinkProductComplementGroup;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.LinkProductComplementGroup;

public sealed class LinkProductComplementGroupCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly LinkProductComplementGroupCommandHandler _handler;

    public LinkProductComplementGroupCommandHandlerTests()
    {
        _handler = new LinkProductComplementGroupCommandHandler(
            _productRepository, _complementGroupRepository, _productComplementGroupRepository, _catalogSyncTrigger,
            _logRepository, _unitOfWork);
        _productComplementGroupRepository.GetByProductForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductComplementGroup>());
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Product CreateProduct(long id = 1, long companyId = 1, bool active = true)
    {
        var product = Product.Create(companyId, 1, 1, "X-Burguer", null, null, 10m, null, false, null).Value;
        if (!active)
            product.Deactivate();
        SetId(product, id);
        return product;
    }

    private static ComplementGroup CreateGroup(long id = 1, long companyId = 1, bool active = true)
    {
        var group = ComplementGroup.Create(companyId, "Bebidas", ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;
        if (!active)
            group.Deactivate();
        SetId(group, id);
        return group;
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        _productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new LinkProductComplementGroupCommand(1, 1, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_GroupNotFound_ShouldReturnFailure()
    {
        var product = CreateProduct();
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _complementGroupRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ComplementGroup?)null);

        var result = await _handler.Handle(new LinkProductComplementGroupCommand(product.Id, 1, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_GroupFromDifferentCompany_ShouldReturnFailure()
    {
        var product = CreateProduct(companyId: 1);
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var group = CreateGroup(companyId: 99);
        _complementGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(new LinkProductComplementGroupCommand(product.Id, group.Id, 0), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_AlreadyLinked_ShouldReturnFailure()
    {
        var product = CreateProduct(companyId: 1);
        var group = CreateGroup(companyId: 1);
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _complementGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        var existingLink = SyncBar.Domain.Entities.ProductComplementGroup.Create(product.Id, group.Id, 0).Value;
        _productComplementGroupRepository.GetByProductForUpdateAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns([existingLink]);

        var result = await _handler.Handle(new LinkProductComplementGroupCommand(product.Id, group.Id, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProductComplementGroup.AlreadyLinked");
    }

    [Fact]
    public async Task Handle_InvalidDisplayOrder_ShouldReturnFailure()
    {
        var product = CreateProduct(companyId: 1);
        var group = CreateGroup(companyId: 1);
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _complementGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(new LinkProductComplementGroupCommand(product.Id, group.Id, -1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ProductComplementGroup.InvalidDisplayOrder");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldLinkAndTriggerSync()
    {
        var product = CreateProduct(companyId: 8);
        var group = CreateGroup(companyId: 8);
        _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _complementGroupRepository.GetByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(new LinkProductComplementGroupCommand(product.Id, group.Id, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _productComplementGroupRepository.Received(1).AddAsync(
            Arg.Is<SyncBar.Domain.Entities.ProductComplementGroup>(l => l.ProductId == product.Id && l.ComplementGroupId == group.Id && l.DisplayOrder == 2),
            Arg.Any<CancellationToken>());
        _catalogSyncTrigger.Received(1).TriggerCompanySync(8);
    }
}
