using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Orders;

// Cobertura das regras de validação (FluentValidation) dos comandos de
// Integrations/Ifood/Orders — sem FluentValidation.TestHelper (não referenciado neste projeto),
// então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodOrdersValidatorsTests
{
    [Fact]
    public void CancelIfoodOrderCommandValidator_WithValidCommand_ShouldBeValid()
        => new CancelIfoodOrderCommandValidator().Validate(new CancelIfoodOrderCommand(1, "CANCEL_REASON")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0, "CANCEL_REASON")]
    [InlineData(1, "")]
    public void CancelIfoodOrderCommandValidator_WithInvalidCommand_ShouldBeInvalid(long orderId, string reason)
        => new CancelIfoodOrderCommandValidator().Validate(new CancelIfoodOrderCommand(orderId, reason)).IsValid.Should().BeFalse();

    [Fact]
    public void AcceptIfoodDisputeCommandValidator_WithValidCommand_ShouldBeValid()
        => new AcceptIfoodDisputeCommandValidator().Validate(new AcceptIfoodDisputeCommand(1, "dispute-1")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0, "dispute-1")]
    [InlineData(1, "")]
    public void AcceptIfoodDisputeCommandValidator_WithInvalidCommand_ShouldBeInvalid(long branchId, string disputeId)
        => new AcceptIfoodDisputeCommandValidator().Validate(new AcceptIfoodDisputeCommand(branchId, disputeId)).IsValid.Should().BeFalse();

    [Fact]
    public void RejectIfoodDisputeCommandValidator_WithValidCommand_ShouldBeValid()
        => new RejectIfoodDisputeCommandValidator().Validate(new RejectIfoodDisputeCommand(1, "dispute-1", "motivo")).IsValid.Should().BeTrue();

    [Fact]
    public void RejectIfoodDisputeCommandValidator_WithoutReason_ShouldBeInvalid()
        => new RejectIfoodDisputeCommandValidator().Validate(new RejectIfoodDisputeCommand(1, "dispute-1", "")).IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodDisputeAlternativeCommandValidator_WithValidCommand_ShouldBeValid()
        => new RequestIfoodDisputeAlternativeCommandValidator()
            .Validate(new RequestIfoodDisputeAlternativeCommand(1, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRL"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void RequestIfoodDisputeAlternativeCommandValidator_WithoutValue_ShouldBeValid()
        => new RequestIfoodDisputeAlternativeCommandValidator()
            .Validate(new RequestIfoodDisputeAlternativeCommand(1, "dispute-1", "alt-1", "RESCHEDULE", null, null))
            .IsValid.Should().BeTrue();

    [Fact]
    public void RequestIfoodDisputeAlternativeCommandValidator_WithZeroAmount_ShouldBeInvalid()
        => new RequestIfoodDisputeAlternativeCommandValidator()
            .Validate(new RequestIfoodDisputeAlternativeCommand(1, "dispute-1", "alt-1", "REFUND_ITEMS", 0m, "BRL"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodDisputeAlternativeCommandValidator_WithCurrencyLongerThanThreeChars_ShouldBeInvalid()
        => new RequestIfoodDisputeAlternativeCommandValidator()
            .Validate(new RequestIfoodDisputeAlternativeCommand(1, "dispute-1", "alt-1", "REFUND_ITEMS", 10m, "BRLX"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodOrderDriverCommandValidator_WithZeroId_ShouldBeInvalid()
        => new RequestIfoodOrderDriverCommandValidator().Validate(new RequestIfoodOrderDriverCommand(0)).IsValid.Should().BeFalse();

    [Fact]
    public void RequestIfoodOrderDriverCommandValidator_WithPositiveId_ShouldBeValid()
        => new RequestIfoodOrderDriverCommandValidator().Validate(new RequestIfoodOrderDriverCommand(1)).IsValid.Should().BeTrue();

    [Fact]
    public void CancelIfoodOrderDriverRequestCommandValidator_WithZeroId_ShouldBeInvalid()
        => new CancelIfoodOrderDriverRequestCommandValidator().Validate(new CancelIfoodOrderDriverRequestCommand(0)).IsValid.Should().BeFalse();

    [Fact]
    public void CancelIfoodOrderDriverRequestCommandValidator_WithPositiveId_ShouldBeValid()
        => new CancelIfoodOrderDriverRequestCommandValidator().Validate(new CancelIfoodOrderDriverRequestCommand(1)).IsValid.Should().BeTrue();

    [Fact]
    public void ValidateIfoodPickupCodeCommandValidator_WithValidCommand_ShouldBeValid()
        => new ValidateIfoodPickupCodeCommandValidator().Validate(new ValidateIfoodPickupCodeCommand(1, "1234")).IsValid.Should().BeTrue();

    [Fact]
    public void ValidateIfoodPickupCodeCommandValidator_WithEmptyCode_ShouldBeInvalid()
        => new ValidateIfoodPickupCodeCommandValidator().Validate(new ValidateIfoodPickupCodeCommand(1, "")).IsValid.Should().BeFalse();

    [Fact]
    public void VerifyIfoodOrderDeliveryCodeCommandValidator_WithValidCommand_ShouldBeValid()
        => new VerifyIfoodOrderDeliveryCodeCommandValidator().Validate(new VerifyIfoodOrderDeliveryCodeCommand(1, "1234")).IsValid.Should().BeTrue();

    [Fact]
    public void VerifyIfoodOrderDeliveryCodeCommandValidator_WithCodeLongerThanTwentyChars_ShouldBeInvalid()
        => new VerifyIfoodOrderDeliveryCodeCommandValidator()
            .Validate(new VerifyIfoodOrderDeliveryCodeCommand(1, new string('9', 21)))
            .IsValid.Should().BeFalse();
}
