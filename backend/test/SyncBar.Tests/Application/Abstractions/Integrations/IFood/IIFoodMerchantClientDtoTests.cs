using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Application.Abstractions.Integrations.IFood;

public sealed class IIFoodMerchantClientDtoTests
{
    [Fact]
    public void IfoodMerchantAddressDto_ShouldExposeConstructorValues()
    {
        var dto = new IfoodMerchantAddressDto(
            Country: "Brasil", State: "SP", City: "Guarulhos", PostalCode: "07020030",
            District: "Vila Moreira", Street: "Rua Santa Luzia", Number: "12345",
            Latitude: -23.45, Longitude: -46.53);

        dto.Should().BeEquivalentTo(new
        {
            Country = "Brasil",
            State = "SP",
            City = "Guarulhos",
            PostalCode = "07020030",
            District = "Vila Moreira",
            Street = "Rua Santa Luzia",
            Number = "12345",
            Latitude = -23.45,
            Longitude = -46.53,
        });
    }

    [Fact]
    public void IfoodMerchantDetailsResult_ShouldExposeConstructorValues()
    {
        var address = new IfoodMerchantAddressDto("Brasil", "SP", "Guarulhos", "07020030", "Centro", "Rua A", "1", null, null);
        var createdAt = new DateTime(2026, 9, 5);

        var dto = new IfoodMerchantDetailsResult(
            Success: true, Id: "merchant-1", Name: "Loja Teste", CorporateName: "Loja Teste LTDA",
            Description: "Descrição", Type: "DEFAULT", Status: "AVAILABLE", CreatedAt: createdAt,
            Address: address, ErrorMessage: null);

        dto.Should().BeEquivalentTo(new
        {
            Success = true,
            Id = "merchant-1",
            Name = "Loja Teste",
            CorporateName = "Loja Teste LTDA",
            Description = "Descrição",
            Type = "DEFAULT",
            Status = "AVAILABLE",
            CreatedAt = createdAt,
            Address = address,
            ErrorMessage = (string?)null,
        });
    }

    [Fact]
    public void IfoodMerchantStatusResult_ShouldExposeConstructorValues()
    {
        var validations = new[] { new IfoodMerchantValidation("id-1", "OPEN", null) };

        var dto = new IfoodMerchantStatusResult(
            Success: true, OperationState: "OK", Available: true, Validations: validations, ErrorMessage: null);

        dto.Should().BeEquivalentTo(new
        {
            Success = true,
            OperationState = "OK",
            Available = true,
            Validations = validations,
            ErrorMessage = (string?)null,
        });
    }

    [Fact]
    public void IfoodMerchantStatusByOperationResult_ShouldExposeConstructorValues()
    {
        var validations = new[] { new IfoodMerchantValidation("id-2", "CLOSED", "Fora do horário") };

        var dto = new IfoodMerchantStatusByOperationResult(
            Success: true, Operation: "DELIVERY", SalesChannel: "IFOOD", Available: false,
            State: "CLOSED", Validations: validations, ErrorMessage: null);

        dto.Should().BeEquivalentTo(new
        {
            Success = true,
            Operation = "DELIVERY",
            SalesChannel = "IFOOD",
            Available = false,
            State = "CLOSED",
            Validations = validations,
            ErrorMessage = (string?)null,
        });
    }
}
