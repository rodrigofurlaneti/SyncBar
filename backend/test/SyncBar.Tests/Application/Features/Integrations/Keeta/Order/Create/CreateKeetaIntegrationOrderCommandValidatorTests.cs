using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Create;

public sealed class CreateKeetaIntegrationOrderCommandValidatorTests
{
    private readonly CreateKeetaIntegrationOrderCommandValidator _validator = new();

    private static CreateKeetaIntegrationOrderCommand ValidCommand() => new(
        CompanyId: 1,
        BranchId: 1,
        CustomerId: 1,
        CustomerOrderId: 1,
        KeetaOrderId: "keeta-order-1",
        DisplayId: "#001",
        InternalMerchantId: "internal-1",
        KeetaMerchantId: 1,
        OrderType: "DELIVERY",
        DeliveredBy: "KEETA",
        OrderAmount: 10m,
        RawOrderJson: "{}",
        OrderCreatedAtUtc: DateTime.UtcNow);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(ValidCommand() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(ValidCommand() with { BranchId = branchId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerOrderId_ShouldBeInvalid(long customerOrderId)
        => _validator.Validate(ValidCommand() with { CustomerOrderId = customerOrderId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyKeetaOrderId_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { KeetaOrderId = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyDisplayId_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { DisplayId = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyInternalMerchantId_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { InternalMerchantId = string.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveKeetaMerchantId_ShouldBeInvalid(long keetaMerchantId)
        => _validator.Validate(ValidCommand() with { KeetaMerchantId = keetaMerchantId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyOrderType_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { OrderType = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyDeliveredBy_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { DeliveredBy = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeOrderAmount_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { OrderAmount = -0.01m }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ZeroOrderAmount_ShouldBeValid()
        => _validator.Validate(ValidCommand() with { OrderAmount = 0m }).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyRawOrderJson_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { RawOrderJson = string.Empty }).IsValid.Should().BeFalse();
}
