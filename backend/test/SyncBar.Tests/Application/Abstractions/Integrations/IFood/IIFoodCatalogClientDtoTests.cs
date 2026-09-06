using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Application.Abstractions.Integrations.IFood;

public sealed class IIFoodCatalogClientDtoTests
{
    [Fact]
    public void IfoodProductDto_ShouldExposeConstructorValues()
    {
        var dto = new IfoodProductDto(
            Id: "prod-1", Name: "X-Burguer", Description: "Descrição", AdditionalInformation: "Sem cebola",
            ExternalCode: "EXT-1", Ean: "789123", Industrialized: false, ImagePath: "/img/1.png");

        dto.Should().BeEquivalentTo(new
        {
            Id = "prod-1",
            Name = "X-Burguer",
            Description = "Descrição",
            AdditionalInformation = "Sem cebola",
            ExternalCode = "EXT-1",
            Ean = "789123",
            Industrialized = false,
            ImagePath = "/img/1.png",
        });
    }

    [Fact]
    public void IfoodUpsertItemOptionGroup_ShouldExposeConstructorValues()
    {
        var groupId = Guid.NewGuid();
        var options = new[] { new IfoodUpsertItemOption(Guid.NewGuid(), Guid.NewGuid(), "Coca-Cola", 6m, true) };

        var dto = new IfoodUpsertItemOptionGroup(
            GroupId: groupId, Name: "Escolha uma bebida", MinOptions: 1, MaxOptions: 1, Options: options);

        dto.Should().BeEquivalentTo(new
        {
            GroupId = groupId,
            Name = "Escolha uma bebida",
            MinOptions = 1,
            MaxOptions = 1,
            Options = options,
        });
    }

    [Fact]
    public void IfoodUpsertItemRequest_ShouldExposeConstructorValues()
    {
        var itemId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var optionGroups = new[]
        {
            new IfoodUpsertItemOptionGroup(Guid.NewGuid(), "Bebidas", 0, 1, Array.Empty<IfoodUpsertItemOption>()),
        };

        var dto = new IfoodUpsertItemRequest(
            ItemId: itemId, IfoodCategoryId: "cat-1", Available: true, Price: 25.5m, ExternalCode: "EXT-1",
            ProductId: productId, ProductName: "X-Burguer", ProductDescription: "Descrição",
            ProductExternalCode: "EXT-PROD-1", OptionGroups: optionGroups);

        dto.Should().BeEquivalentTo(new
        {
            ItemId = itemId,
            IfoodCategoryId = "cat-1",
            Available = true,
            Price = 25.5m,
            ExternalCode = "EXT-1",
            ProductId = productId,
            ProductName = "X-Burguer",
            ProductDescription = "Descrição",
            ProductExternalCode = "EXT-PROD-1",
            OptionGroups = optionGroups,
        });
    }

    [Fact]
    public void IfoodUpsertProductRequest_ShouldExposeConstructorValues()
    {
        var shifts = new[] { new IfoodProductShift("08:00", "22:00", true, true, true, true, true, false, false) };

        var dto = new IfoodUpsertProductRequest(
            Id: "prod-1", Name: "X-Burguer", Description: "Descrição", AdditionalInformation: "Sem cebola",
            ExternalCode: "EXT-1", Ean: "789123", Image: "base64img", Shifts: shifts);

        dto.Should().BeEquivalentTo(new
        {
            Id = "prod-1",
            Name = "X-Burguer",
            Description = "Descrição",
            AdditionalInformation = "Sem cebola",
            ExternalCode = "EXT-1",
            Ean = "789123",
            Image = "base64img",
            Shifts = shifts,
        });
    }
}
