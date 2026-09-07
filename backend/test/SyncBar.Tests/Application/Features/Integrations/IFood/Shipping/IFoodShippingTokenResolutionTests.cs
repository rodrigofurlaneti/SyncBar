using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Shipping;

public sealed class IFoodShippingTokenResolutionTests
{
    private const long ShippingDeliveryId = 1;
    private const long BranchId = 10;
    private const long CompanyId = 1;

    private readonly IIfoodShippingDeliveryRepository _deliveryRepository = Substitute.For<IIfoodShippingDeliveryRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IIfoodTokenProvider _tokenProvider = Substitute.For<IIfoodTokenProvider>();

    private static IfoodShippingDelivery CreateDelivery(long branchId = BranchId) =>
        IfoodShippingDelivery.Create(
            branchId, "order-ref-1", "Cliente Teste", "11", "999998888", "01310000", "Av. Paulista", "1000", null,
            "Bela Vista", "São Paulo", "SP", "BR", null, null, null, 15m, "quote-1", "delivery-1", null, DateTime.UtcNow).Value;

    private static Branch CreateBranch(long companyId = CompanyId)
        => Branch.Create(companyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    private Task<SyncBar.Domain.Primitives.Result<(IfoodShippingDelivery Delivery, string Token)>> Resolve()
        => IfoodShippingTokenResolution.ResolveAsync(ShippingDeliveryId, _deliveryRepository, _branchRepository, _tokenProvider, CancellationToken.None);

    [Fact]
    public async Task ResolveAsync_DeliveryNotFound_ShouldReturnFailure()
    {
        _deliveryRepository.GetByIdAsync(ShippingDeliveryId, Arg.Any<CancellationToken>()).Returns((IfoodShippingDelivery?)null);

        var result = await Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("IfoodShippingDelivery.NotFound");
    }

    [Fact]
    public async Task ResolveAsync_BranchNotFound_ShouldReturnFailure()
    {
        var delivery = CreateDelivery();
        _deliveryRepository.GetByIdAsync(ShippingDeliveryId, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task ResolveAsync_TokenNull_ShouldReturnNotConnected()
    {
        var delivery = CreateDelivery();
        _deliveryRepository.GetByIdAsync(ShippingDeliveryId, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((string?)null);

        var result = await Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Ifood.NotConnected");
    }

    [Fact]
    public async Task ResolveAsync_Success_ShouldReturnDeliveryAndToken()
    {
        var delivery = CreateDelivery();
        _deliveryRepository.GetByIdAsync(ShippingDeliveryId, Arg.Any<CancellationToken>()).Returns(delivery);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(CreateBranch());
        _tokenProvider.GetAccessTokenAsync(CompanyId, Arg.Any<CancellationToken>()).Returns("valid-token");

        var result = await Resolve();

        result.IsSuccess.Should().BeTrue();
        result.Value.Delivery.Should().BeSameAs(delivery);
        result.Value.Token.Should().Be("valid-token");
    }
}
