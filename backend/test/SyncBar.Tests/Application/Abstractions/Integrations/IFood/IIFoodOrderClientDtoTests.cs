using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Application.Abstractions.Integrations.IFood;

public sealed class IIFoodOrderClientDtoTests
{
    [Fact]
    public void IfoodOrderItemDto_ShouldExposeConstructorValues()
    {
        var options = new[] { new IfoodOrderItemOptionDto("opt-1", "Refrigerante", 1m, 5m) };

        var dto = new IfoodOrderItemDto(
            ExternalCode: "EXT-1", Ean: "789123", Name: "X-Burguer", Quantity: 2m, UnitPrice: 25.5m, Options: options);

        dto.Should().BeEquivalentTo(new
        {
            ExternalCode = "EXT-1",
            Ean = "789123",
            Name = "X-Burguer",
            Quantity = 2m,
            UnitPrice = 25.5m,
            Options = options,
        });
    }

    [Fact]
    public void IfoodOrderDetailsDto_ShouldExposeConstructorValues()
    {
        var items = new[]
        {
            new IfoodOrderItemDto("EXT-1", "789123", "X-Burguer", 1m, 25.5m, Array.Empty<IfoodOrderItemOptionDto>()),
        };
        var createdAt = new DateTime(2026, 9, 5, 12, 0, 0);
        var prepStart = createdAt.AddMinutes(2);

        var dto = new IfoodOrderDetailsDto(
            Id: "order-1", DisplayId: "#0001", OrderType: "DELIVERY", OrderTiming: "IMMEDIATE",
            Category: "FOOD", CreatedAt: createdAt, PreparationStartDateTime: prepStart,
            MerchantId: "merchant-1", CustomerName: "Fabio Cardoso", CustomerPhone: "11999999999",
            DeliveryAddressFormatted: "Rua Santa Luzia, 12345", DeliveredBy: "IFOOD", TakeoutMode: null,
            OrderAmount: 25.5m, Items: items);

        dto.Should().BeEquivalentTo(new
        {
            Id = "order-1",
            DisplayId = "#0001",
            OrderType = "DELIVERY",
            OrderTiming = "IMMEDIATE",
            Category = "FOOD",
            CreatedAt = createdAt,
            PreparationStartDateTime = prepStart,
            MerchantId = "merchant-1",
            CustomerName = "Fabio Cardoso",
            CustomerPhone = "11999999999",
            DeliveryAddressFormatted = "Rua Santa Luzia, 12345",
            DeliveredBy = "IFOOD",
            TakeoutMode = (string?)null,
            OrderAmount = 25.5m,
            Items = items,
        });
    }

    [Fact]
    public void IfoodOrderTrackingDto_ShouldExposeConstructorValues()
    {
        var expectedDelivery = new DateTime(2026, 9, 5, 13, 0, 0);

        var dto = new IfoodOrderTrackingDto(
            Latitude: -23.45, Longitude: -46.53, ExpectedDelivery: expectedDelivery,
            DeliveryEtaEndMinutes: 30, PickupEtaStartMinutes: 5);

        dto.Should().BeEquivalentTo(new
        {
            Latitude = -23.45,
            Longitude = -46.53,
            ExpectedDelivery = expectedDelivery,
            DeliveryEtaEndMinutes = 30d,
            PickupEtaStartMinutes = 5d,
        });
    }

    [Fact]
    public void IfoodVirtualBagResult_ShouldExposeConstructorValues()
    {
        var items = new[] { new IfoodVirtualBagItemDto("uid-1", "Arroz", 2, "789456") };
        var createdAt = new DateTime(2026, 9, 5);

        var dto = new IfoodVirtualBagResult(
            Success: true, Id: "bag-1", ShortCode: "ABC123", Status: "PENDING", CreatedAt: createdAt,
            MerchantName: "Loja Teste", CustomerName: "Fabio Cardoso", Items: items,
            GrossValueAmount: "50.00", GrossValueCurrency: "BRL", RawPayload: "{}", ErrorMessage: null);

        dto.Should().BeEquivalentTo(new
        {
            Success = true,
            Id = "bag-1",
            ShortCode = "ABC123",
            Status = "PENDING",
            CreatedAt = createdAt,
            MerchantName = "Loja Teste",
            CustomerName = "Fabio Cardoso",
            Items = items,
            GrossValueAmount = "50.00",
            GrossValueCurrency = "BRL",
            RawPayload = "{}",
            ErrorMessage = (string?)null,
        });
    }
}
