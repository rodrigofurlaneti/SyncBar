using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Tenancy;
using SyncBar.Application.Features.Catalog.Complements.CreateComplementItem;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.CreateComplementItem;

public sealed class CreateComplementItemCommandHandlerTests
{
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICurrentTenantService _currentTenant = Substitute.For<ICurrentTenantService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateComplementItemCommandHandler _handler;

    public CreateComplementItemCommandHandlerTests()
    {
        _handler = new CreateComplementItemCommandHandler(
            _complementItemRepository, _productRepository, _currentTenant, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Product CreateProduct(long id, long companyId, bool active = true)
    {
        var product = Product.Create(companyId, 1, 1, "X-Burguer", null, null, 10m, null, false, null).Value;
        if (!active)
            product.Deactivate();
        SetId(product, id);
        return product;
    }

    [Fact]
    public async Task Handle_NoTenant_ShouldReturnForbidden()
    {
        _currentTenant.CompanyId.Returns((long?)null);

        var result = await _handler.Handle(new CreateComplementItemCommand(1, "Bacon"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.Forbidden");
    }

    [Fact]
    public async Task Handle_TenantMismatch_ShouldReturnForbidden()
    {
        _currentTenant.CompanyId.Returns(2L);

        var result = await _handler.Handle(new CreateComplementItemCommand(1, "Bacon"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.Forbidden");
    }

    [Fact]
    public async Task Handle_LinkedProductNotFound_ShouldReturnFailure()
    {
        _currentTenant.CompanyId.Returns(1L);
        _productRepository.GetByIdAsync(50, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new CreateComplementItemCommand(1, "Combo", 50), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_LinkedProductFromDifferentCompany_ShouldReturnFailure()
    {
        _currentTenant.CompanyId.Returns(1L);
        var product = CreateProduct(50, companyId: 99);
        _productRepository.GetByIdAsync(50, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new CreateComplementItemCommand(1, "Combo", 50), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailure()
    {
        _currentTenant.CompanyId.Returns(1L);

        var result = await _handler.Handle(new CreateComplementItemCommand(1, ""), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementItem.EmptyName");
    }

    [Fact]
    public async Task Handle_ValidCommandWithoutLinkedProduct_ShouldPersist()
    {
        _currentTenant.CompanyId.Returns(1L);

        var result = await _handler.Handle(new CreateComplementItemCommand(1, "Bacon"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _complementItemRepository.Received(1).AddAsync(
            Arg.Is<ComplementItem>(i => i.Name == "Bacon" && i.LinkedProductId == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommandWithLinkedProduct_ShouldPersist()
    {
        _currentTenant.CompanyId.Returns(1L);
        var product = CreateProduct(50, companyId: 1);
        _productRepository.GetByIdAsync(50, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new CreateComplementItemCommand(1, "X-Salada (combo)", 50), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _complementItemRepository.Received(1).AddAsync(
            Arg.Is<ComplementItem>(i => i.LinkedProductId == 50), Arg.Any<CancellationToken>());
    }
}
