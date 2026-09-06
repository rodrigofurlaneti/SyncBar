using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Application.Abstractions.Integrations.IFood;

public sealed class IIFoodShippingClientDtoTests
{
    [Fact]
    public void IfoodShippingQuoteResult_ShouldExposeConstructorValues()
    {
        var expirationAt = new DateTime(2026, 9, 5, 12, 0, 0);

        var dto = new IfoodShippingQuoteResult(
            Success: true, ErrorMessage: null, QuoteId: "quote-1", GrossValue: 20m, Discount: 2m,
            NetValue: 18m, DeliveryTimeMinMinutes: 15, DeliveryTimeMaxMinutes: 30, DistanceMeters: 3200,
            ExpirationAt: expirationAt);

        dto.Should().BeEquivalentTo(new
        {
            Success = true,
            ErrorMessage = (string?)null,
            QuoteId = "quote-1",
            GrossValue = 20m,
            Discount = 2m,
            NetValue = 18m,
            DeliveryTimeMinMinutes = 15d,
            DeliveryTimeMaxMinutes = 30d,
            DistanceMeters = 3200,
            ExpirationAt = (DateTime?)expirationAt,
        });
    }

    [Fact]
    public void IfoodShippingItemPayload_ShouldExposeConstructorValues()
    {
        var dto = new IfoodShippingItemPayload(
            Name: "X-Burguer", ExternalCode: "EXT-1", Quantity: 2, UnitPrice: 25.5m, Price: 51m, TotalPrice: 51m);

        dto.Should().BeEquivalentTo(new
        {
            Name = "X-Burguer",
            ExternalCode = "EXT-1",
            Quantity = 2,
            UnitPrice = 25.5m,
            Price = 51m,
            TotalPrice = 51m,
        });
    }

    [Fact]
    public void IfoodShippingRequestDriverPayload_ShouldExposeConstructorValues()
    {
        var items = new[] { new IfoodShippingItemPayload("X-Burguer", "EXT-1", 1, 25.5m, 25.5m, 25.5m) };

        var dto = new IfoodShippingRequestDriverPayload(
            CustomerName: "Fabio Cardoso", CustomerPhoneAreaCode: "11", CustomerPhoneNumber: "999999999",
            MerchantFee: 5m, QuoteId: "quote-1", PostalCode: "07020030", StreetNumber: "12345",
            StreetName: "Rua Santa Luzia", Complement: "Apto 1", Neighborhood: "Vila Moreira",
            City: "Guarulhos", State: "SP", Country: "BR", Reference: "Perto do mercado",
            Latitude: -23.45, Longitude: -46.53, Items: items);

        dto.Should().BeEquivalentTo(new
        {
            CustomerName = "Fabio Cardoso",
            CustomerPhoneAreaCode = "11",
            CustomerPhoneNumber = "999999999",
            MerchantFee = 5m,
            QuoteId = "quote-1",
            PostalCode = "07020030",
            StreetNumber = "12345",
            StreetName = "Rua Santa Luzia",
            Complement = "Apto 1",
            Neighborhood = "Vila Moreira",
            City = "Guarulhos",
            State = "SP",
            Country = "BR",
            Reference = "Perto do mercado",
            Latitude = (double?)(-23.45),
            Longitude = (double?)(-46.53),
            Items = items,
        });
    }

    [Fact]
    public void IfoodShippingTrackingResult_ShouldExposeConstructorValues()
    {
        var expectedDelivery = new DateTime(2026, 9, 5, 13, 0, 0);

        var dto = new IfoodShippingTrackingResult(
            Success: true, ErrorMessage: null, Latitude: -23.45, Longitude: -46.53,
            ExpectedDelivery: expectedDelivery, DeliveryEtaEndMinutes: 30, PickupEtaStartMinutes: 5);

        dto.Should().BeEquivalentTo(new
        {
            Success = true,
            ErrorMessage = (string?)null,
            Latitude = (double?)(-23.45),
            Longitude = (double?)(-46.53),
            ExpectedDelivery = (DateTime?)expectedDelivery,
            DeliveryEtaEndMinutes = (double?)30,
            PickupEtaStartMinutes = (double?)5,
        });
    }

    [Fact]
    public void IfoodShippingDeliveryAddressChangePayload_ShouldExposeConstructorValues()
    {
        var dto = new IfoodShippingDeliveryAddressChangePayload(
            StreetNumber: "12345", StreetName: "Rua Santa Luzia", Complement: "Apto 1",
            Neighborhood: "Vila Moreira", City: "Guarulhos", State: "SP", Country: "BR",
            Reference: "Perto do mercado", Latitude: -23.45, Longitude: -46.53);

        dto.Should().BeEquivalentTo(new
        {
            StreetNumber = "12345",
            StreetName = "Rua Santa Luzia",
            Complement = "Apto 1",
            Neighborhood = "Vila Moreira",
            City = "Guarulhos",
            State = "SP",
            Country = "BR",
            Reference = "Perto do mercado",
            Latitude = (double?)(-23.45),
            Longitude = (double?)(-46.53),
        });
    }
}
