using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Shipping;

public sealed class GetIfoodShippingDeliveriesQueryHandlerTests
{
    private readonly IIfoodShippingDeliveryRepository _deliveryRepository = Substitute.For<IIfoodShippingDeliveryRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodShippingDeliveriesQueryHandler _handler;

    public GetIfoodShippingDeliveriesQueryHandlerTests()
    {
        _handler = new GetIfoodShippingDeliveriesQueryHandler(_deliveryRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoOpenDeliveries_ShouldReturnEmptyList()
    {
        _deliveryRepository.GetOpenByBranchAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetIfoodShippingDeliveriesQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeliveriesExist_ShouldFormatAddressWithComplement()
    {
        var delivery = IfoodShippingDelivery.Create(
            1, "order-ref-1", "Cliente Teste", "11", "999998888", "01310000", "Av. Paulista", "1000", "Apto 2",
            "Bela Vista", "São Paulo", "SP", "BR", null, null, null, 15m, "quote-1", "delivery-1", "https://track", DateTime.UtcNow).Value;
        _deliveryRepository.GetOpenByBranchAsync(1, Arg.Any<CancellationToken>()).Returns([delivery]);

        var result = await _handler.Handle(new GetIfoodShippingDeliveriesQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d =>
            d.DeliveryAddress == "Av. Paulista, 1000 - Apto 2 — Bela Vista, São Paulo/SP" && d.TrackingUrl == "https://track");
    }

    [Fact]
    public async Task Handle_DeliveryWithoutComplement_ShouldOmitComplementFromAddress()
    {
        var delivery = IfoodShippingDelivery.Create(
            1, "order-ref-1", "Cliente Teste", "11", "999998888", "01310000", "Av. Paulista", "1000", null,
            "Bela Vista", "São Paulo", "SP", "BR", null, null, null, 15m, "quote-1", "delivery-1", null, DateTime.UtcNow).Value;
        _deliveryRepository.GetOpenByBranchAsync(1, Arg.Any<CancellationToken>()).Returns([delivery]);

        var result = await _handler.Handle(new GetIfoodShippingDeliveriesQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(d => d.DeliveryAddress == "Av. Paulista, 1000 — Bela Vista, São Paulo/SP");
    }
}
