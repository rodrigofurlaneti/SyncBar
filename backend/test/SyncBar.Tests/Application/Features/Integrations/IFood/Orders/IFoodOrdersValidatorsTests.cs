using FluentAssertions;
using SyncBar.Application.Features.Integrations.IFood.Orders;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Orders;

// Cobertura das regras de validação (FluentValidation) dos comandos de
// Integrations/IFood/Orders — sem FluentValidation.TestHelper (não referenciado neste projeto),
// então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IFoodOrdersValidatorsTests
{
    [Fact]
    public void CancelIFoodOrderCommandValidator_WithValidCommand_ShouldBeValid()
        => new CancelIFoodOrderCommandValidator().Validate(new CancelIFoodOrderCommand(1, "CANCEL_REASON")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0, "CANCEL_REASON")]
    [InlineData(1, "")]
    public void CancelIFoodOrderCommandValidator_WithInvalidCommand_ShouldBeInvalid(long orderId, string reason)
        => new CancelIFoodOrderCommandValidator().Validate(new CancelIFoodOrderCommand(orderId, reason)).IsValid.Should().BeFalse();

    [Fact]
    public void AcceptIFoodDisputeCommandValidator_WithValidCommand_ShouldBeValid()
        => new AcceptIFoodDisputeCommandValidator().Validate(new AcceptIFoodDisputeCommand(1, "dispute-1")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0, "dispute-1")]
    [InlineData(1, "")]
    public void AcceptIFoodDisputeCommandValidator_WithInvalidCommand_ShouldBeInvalid(long branchId, string disputeId)
        => new AcceptIFoodDisputeCommandValidator().Validate(new AcceptIFoodDisputeCommand(branchId, disputeId)).IsValid.Should().BeFalse();

    [Fact]
    public void RejectIFoodDisputeCommandValidator_WithValidCommand_ShouldBeValid()
        => new RejectIFoodDisputeCommandValidator().Validate(new RejectIFoodDisputeCommand(1, "dispute-1", "motivo")).IsValid.Should().BeTrue();

    [Fact]
    public void RejectIFoodDisputeCommandValidator_WithoutReason_ShouldBeInvalid()
        => new RejectIFoodDisputeCommandValidator().Validate(new RejectIFoodDisputeCommand(1, "dispute-1", "")).IsValid.Should().BeFalse();

    [Fact]
    public void RequestIFoodDisputeAlternativeCommandValidator_WithValidCommand_ShouldBeValid()
        => new RequestIFoodDisputeAlternativeCommandValidator()
            .Validate(new RequestIFoodDisputeAlternativeCommand(1, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void RequestIFoodDisputeAlternativeCommandValidator_WithoutValue_ShouldBeValid()
        => new RequestIFoodDisputeAlternativeCommandValidator()
            .Validate(new RequestIFoodDisputeAlternativeCommand(1, "dispute-1", "alt-1", "RESCHEDULE", null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void RequestIFoodDisputeAlternativeCommandValidator_WithZeroAmount_ShouldBeInvalid()
        => new RequestIFoodDisputeAlternativeCommandValidator()
            .Validate(new RequestIFoodDisputeAlternativeCommand(1, "dispute-1", "alt-1", "REFUND_ITEMS", 0m, "BRL"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIFoodDisputeAlternativeCommandValidator_WithCurrencyLongerThanThreeChars_ShouldBeInvalid()
        => new RequestIFoodDisputeAlternativeCommandValidator()
            .Validate(new RequestIFoodDisputeAlternativeCommand(1, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRLX"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIFoodOrderDriverCommandValidator_WithZeroId_ShouldBeInvalid()
        => new RequestIFoodOrderDriverCommandValidator().Validate(new RequestIFoodOrderDriverCommand(0)).IsValid.Should().BeFalse();

    [Fact]
    public void RequestIFoodOrderDriverCommandValidator_WithPositiveId_ShouldBeValid()
        => new RequestIFoodOrderDriverCommandValidator().Validate(new RequestIFoodOrderDriverCommand(1)).IsValid.Should().BeTrue();

    [Fact]
    public void CancelIFoodOrderDriverRequestCommandValidator_WithZeroId_ShouldBeInvalid()
        => new CancelIFoodOrderDriverRequestCommandValidator().Validate(new CancelIFoodOrderDriverRequestCommand(0)).IsValid.Should().BeFalse();

    [Fact]
    public void CancelIFoodOrderDriverRequestCommandValidator_WithPositiveId_ShouldBeValid()
        => new CancelIFoodOrderDriverRequestCommandValidator().Validate(new CancelIFoodOrderDriverRequestCommand(1)).IsValid.Should().BeTrue();

    [Fact]
    public void ValidateIFoodPickupCodeCommandValidator_WithValidCommand_ShouldBeValid()
        => new ValidateIFoodPickupCodeCommandValidator().Validate(new ValidateIFoodPickupCodeCommand(1, "1234")).IsValid.Should().BeTrue();

    [Fact]
    public void ValidateIFoodPickupCodeCommandValidator_WithEmptyCode_ShouldBeInvalid()
        => new ValidateIFoodPickupCodeCommandValidator().Validate(new ValidateIFoodPickupCodeCommand(1, "")).IsValid.Should().BeFalse();

    [Fact]
    public void VerifyIFoodOrderDeliveryCodeCommandValidator_WithValidCommand_ShouldBeValid()
        => new VerifyIFoodOrderDeliveryCodeCommandValidator().Validate(new VerifyIFoodOrderDeliveryCodeCommand(1, "1234")).IsValid.Should().BeTrue();

    [Fact]
    public void VerifyIFoodOrderDeliveryCodeCommandValidator_WithCodeLongerThanTwentyChars_ShouldBeInvalid()
        => new VerifyIFoodOrderDeliveryCodeCommandValidator()
            .Validate(new VerifyIFoodOrderDeliveryCodeCommand(1, new string('9', 21)))
            .IsValid.Should().BeFalse();
}
