using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Shipping;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Shipping;

// Cobertura das regras de validação (FluentValidation) dos comandos de
// Integrations/Ifood/Shipping — sem FluentValidation.TestHelper (não referenciado neste projeto),
// então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodShippingValidatorsTests
{
    private static RequestIfoodShippingDriverCommand ValidShippingDriverCommand()
        => new(
            BranchId: 1,
            OrderReference: "REF-1",
            CustomerName: "Cliente Teste",
            CustomerPhoneAreaCode: "11",
            CustomerPhoneNumber: "999999999",
            MerchantFee: 5m,
            QuoteId: "quote-1",
            PostalCode: "01000-000",
            StreetNumber: "100",
            StreetName: "Rua Teste",
            Complement: null,
            Neighborhood: "Centro",
            City: "São Paulo",
            State: "SP",
            Country: null,
            Reference: null,
            Latitude: null,
            Longitude: null,
            Items: [new IfoodShippingItemInput("Item 1", null, 1, 10m)]);

    [Fact]
    public void CancelIfoodShippingDeliveryCommandValidator_WithValidCommand_ShouldBeValid()
        => new CancelIfoodShippingDeliveryCommandValidator()
            .Validate(new CancelIfoodShippingDeliveryCommand(1, "Cliente desistiu", 0))
            .IsValid.Should().BeTrue();

    [Fact]
    public void CancelIfoodShippingDeliveryCommandValidator_WithZeroId_ShouldBeInvalid()
        => new CancelIfoodShippingDeliveryCommandValidator()
            .Validate(new CancelIfoodShippingDeliveryCommand(0, "Cliente desistiu", 0))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CancelIfoodShippingDeliveryCommandValidator_WithEmptyReason_ShouldBeInvalid()
        => new CancelIfoodShippingDeliveryCommandValidator()
            .Validate(new CancelIfoodShippingDeliveryCommand(1, "", 0))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CancelIfoodShippingDeliveryCommandValidator_WithReasonLongerThan300Chars_ShouldBeInvalid()
        => new CancelIfoodShippingDeliveryCommandValidator()
            .Validate(new CancelIfoodShippingDeliveryCommand(1, new string('A', 301), 0))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CancelIfoodShippingDeliveryCommandValidator_WithNegativeCancellationCode_ShouldBeInvalid()
        => new CancelIfoodShippingDeliveryCommandValidator()
            .Validate(new CancelIfoodShippingDeliveryCommand(1, "Cliente desistiu", -1))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestDeliveryAddressChangeCommandValidator_WithValidCommand_ShouldBeValid()
        => new RequestDeliveryAddressChangeCommandValidator()
            .Validate(new RequestDeliveryAddressChangeCommand(1, "100", "Rua Teste", null, "Centro", "São Paulo", "SP", null, null, null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void RequestDeliveryAddressChangeCommandValidator_WithZeroOrderId_ShouldBeInvalid()
        => new RequestDeliveryAddressChangeCommandValidator()
            .Validate(new RequestDeliveryAddressChangeCommand(0, "100", "Rua Teste", null, "Centro", "São Paulo", "SP", null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestDeliveryAddressChangeCommandValidator_WithEmptyStreetNumber_ShouldBeInvalid()
        => new RequestDeliveryAddressChangeCommandValidator()
            .Validate(new RequestDeliveryAddressChangeCommand(1, "", "Rua Teste", null, "Centro", "São Paulo", "SP", null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestDeliveryAddressChangeCommandValidator_WithEmptyStreetName_ShouldBeInvalid()
        => new RequestDeliveryAddressChangeCommandValidator()
            .Validate(new RequestDeliveryAddressChangeCommand(1, "100", "", null, "Centro", "São Paulo", "SP", null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestDeliveryAddressChangeCommandValidator_WithEmptyNeighborhood_ShouldBeInvalid()
        => new RequestDeliveryAddressChangeCommandValidator()
            .Validate(new RequestDeliveryAddressChangeCommand(1, "100", "Rua Teste", null, "", "São Paulo", "SP", null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestDeliveryAddressChangeCommandValidator_WithEmptyCity_ShouldBeInvalid()
        => new RequestDeliveryAddressChangeCommandValidator()
            .Validate(new RequestDeliveryAddressChangeCommand(1, "100", "Rua Teste", null, "Centro", "", "SP", null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestDeliveryAddressChangeCommandValidator_WithEmptyState_ShouldBeInvalid()
        => new RequestDeliveryAddressChangeCommandValidator()
            .Validate(new RequestDeliveryAddressChangeCommand(1, "100", "Rua Teste", null, "Centro", "São Paulo", "", null, null, null, null))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodOrderShippingDriverCommandValidator_WithValidCommand_ShouldBeValid()
        => new RequestIfoodOrderShippingDriverCommandValidator()
            .Validate(new RequestIfoodOrderShippingDriverCommand(1, "quote-1"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void RequestIfoodOrderShippingDriverCommandValidator_WithZeroOrderId_ShouldBeInvalid()
        => new RequestIfoodOrderShippingDriverCommandValidator()
            .Validate(new RequestIfoodOrderShippingDriverCommand(0, "quote-1"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodOrderShippingDriverCommandValidator_WithEmptyQuoteId_ShouldBeInvalid()
        => new RequestIfoodOrderShippingDriverCommandValidator()
            .Validate(new RequestIfoodOrderShippingDriverCommand(1, ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithValidCommand_ShouldBeValid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand())
            .IsValid.Should().BeTrue();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { BranchId = 0 })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithOrderReferenceLongerThan150Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { OrderReference = new string('A', 151) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyCustomerName_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { CustomerName = "" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithCustomerNameLongerThan150Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { CustomerName = new string('A', 151) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyCustomerPhoneAreaCode_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { CustomerPhoneAreaCode = "" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithCustomerPhoneAreaCodeLongerThan5Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { CustomerPhoneAreaCode = "123456" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyCustomerPhoneNumber_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { CustomerPhoneNumber = "" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithCustomerPhoneNumberLongerThan20Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { CustomerPhoneNumber = new string('9', 21) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithNegativeMerchantFee_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { MerchantFee = -1m })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyQuoteId_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { QuoteId = "" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyPostalCode_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { PostalCode = "" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithPostalCodeLongerThan15Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { PostalCode = new string('0', 16) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyStreetNumber_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { StreetNumber = "" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithStreetNumberLongerThan20Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { StreetNumber = new string('1', 21) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyStreetName_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { StreetName = "" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithStreetNameLongerThan200Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { StreetName = new string('A', 201) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithComplementLongerThan100Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { Complement = new string('A', 101) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyNeighborhood_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { Neighborhood = "" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithNeighborhoodLongerThan100Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { Neighborhood = new string('A', 101) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyCity_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { City = "" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithCityLongerThan100Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { City = new string('A', 101) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyState_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { State = "" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithStateLongerThan2Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { State = "SPX" })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithReferenceLongerThan200Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { Reference = new string('A', 201) })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithEmptyItems_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { Items = [] })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithItemEmptyName_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { Items = [new IfoodShippingItemInput("", null, 1, 10m)] })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithItemNameLongerThan200Chars_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { Items = [new IfoodShippingItemInput(new string('A', 201), null, 1, 10m)] })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithItemZeroQuantity_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { Items = [new IfoodShippingItemInput("Item 1", null, 0, 10m)] })
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodShippingDriverCommandValidator_WithItemNegativeUnitPrice_ShouldBeInvalid()
        => new RequestIfoodShippingDriverCommandValidator()
            .Validate(ValidShippingDriverCommand() with { Items = [new IfoodShippingItemInput("Item 1", null, 1, -1m)] })
            .IsValid.Should().BeFalse();
}
