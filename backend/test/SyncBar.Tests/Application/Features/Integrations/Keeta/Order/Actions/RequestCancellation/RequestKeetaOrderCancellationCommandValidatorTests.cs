using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RequestCancellation;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.RequestCancellation;

public sealed class RequestKeetaOrderCancellationCommandValidatorTests
{
    private readonly RequestKeetaOrderCancellationCommandValidator _validator = new();

    private static RequestKeetaOrderCancellationCommand ValidCommand() => new(
        OrderId: 1,
        Reason: "reason",
        Code: "SYSTEMIC_ISSUES",
        Mode: "AUTO");

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveOrderId_ShouldBeInvalid(long orderId)
        => _validator.Validate(ValidCommand() with { OrderId = orderId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyReason_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { Reason = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_InvalidCode_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { Code = "INVALID_CODE" }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("SYSTEMIC_ISSUES")]
    [InlineData("DUPLICATE_APPLICATION")]
    [InlineData("UNAVAILABLE_ITEM")]
    [InlineData("RESTAURANT_WITHOUT_DELIVERY_PERSON")]
    [InlineData("OUTDATED_MENU")]
    [InlineData("ORDER_OUTSIDE_THE_DELIVERY_AREA")]
    [InlineData("BLOCKED_CUSTOMER")]
    [InlineData("OUTSIDE_DELIVERY_HOURS")]
    [InlineData("INTERNAL_DIFFICULTIES_OF_THE_RESTAURANT")]
    [InlineData("RISK_AREA")]
    [InlineData("DELIVERY_PROBLEM")]
    public void Validate_ValidCodes_ShouldBeValid(string code)
        => _validator.Validate(ValidCommand() with { Code = code }).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_InvalidMode_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { Mode = "INVALID_MODE" }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("AUTO")]
    [InlineData("MANUAL")]
    public void Validate_ValidModes_ShouldBeValid(string mode)
        => _validator.Validate(ValidCommand() with { Mode = mode }).IsValid.Should().BeTrue();
}
