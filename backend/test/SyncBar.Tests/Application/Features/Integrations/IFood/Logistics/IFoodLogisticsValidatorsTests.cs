using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Logistics;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Logistics;

// Cobertura das regras de validação (FluentValidation) dos comandos de
// Integrations/Ifood/Logistics — sem FluentValidation.TestHelper (não referenciado neste projeto),
// então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodLogisticsValidatorsTests
{
    [Fact]
    public void AssignIfoodDriverCommandValidator_WithValidCommand_ShouldBeValid()
        => new AssignIfoodDriverCommandValidator()
            .Validate(new AssignIfoodDriverCommand(1, "Joao Silva", "11999999999", "MOTORCYCLE"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void AssignIfoodDriverCommandValidator_WithZeroOrderId_ShouldBeInvalid()
        => new AssignIfoodDriverCommandValidator()
            .Validate(new AssignIfoodDriverCommand(0, "Joao Silva", "11999999999", "MOTORCYCLE"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void AssignIfoodDriverCommandValidator_WithEmptyDriverName_ShouldBeInvalid()
        => new AssignIfoodDriverCommandValidator()
            .Validate(new AssignIfoodDriverCommand(1, "", "11999999999", "MOTORCYCLE"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void AssignIfoodDriverCommandValidator_WithDriverNameLongerThan150Chars_ShouldBeInvalid()
        => new AssignIfoodDriverCommandValidator()
            .Validate(new AssignIfoodDriverCommand(1, new string('A', 151), "11999999999", "MOTORCYCLE"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void AssignIfoodDriverCommandValidator_WithEmptyDriverPhone_ShouldBeInvalid()
        => new AssignIfoodDriverCommandValidator()
            .Validate(new AssignIfoodDriverCommand(1, "Joao Silva", "", "MOTORCYCLE"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void AssignIfoodDriverCommandValidator_WithDriverPhoneLongerThan30Chars_ShouldBeInvalid()
        => new AssignIfoodDriverCommandValidator()
            .Validate(new AssignIfoodDriverCommand(1, "Joao Silva", new string('9', 31), "MOTORCYCLE"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void AssignIfoodDriverCommandValidator_WithEmptyVehicleType_ShouldBeInvalid()
        => new AssignIfoodDriverCommandValidator()
            .Validate(new AssignIfoodDriverCommand(1, "Joao Silva", "11999999999", ""))
            .IsValid.Should().BeFalse();

    [Fact]
    public void AssignIfoodDriverCommandValidator_WithVehicleTypeLongerThan30Chars_ShouldBeInvalid()
        => new AssignIfoodDriverCommandValidator()
            .Validate(new AssignIfoodDriverCommand(1, "Joao Silva", "11999999999", new string('X', 31)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void DispatchIfoodLogisticsCommandValidator_WithValidCommand_ShouldBeValid()
        => new DispatchIfoodLogisticsCommandValidator().Validate(new DispatchIfoodLogisticsCommand(1)).IsValid.Should().BeTrue();

    [Fact]
    public void DispatchIfoodLogisticsCommandValidator_WithZeroOrderId_ShouldBeInvalid()
        => new DispatchIfoodLogisticsCommandValidator().Validate(new DispatchIfoodLogisticsCommand(0)).IsValid.Should().BeFalse();

    [Fact]
    public void MarkIfoodArrivedAtDestinationCommandValidator_WithValidCommand_ShouldBeValid()
        => new MarkIfoodArrivedAtDestinationCommandValidator().Validate(new MarkIfoodArrivedAtDestinationCommand(1)).IsValid.Should().BeTrue();

    [Fact]
    public void MarkIfoodArrivedAtDestinationCommandValidator_WithZeroOrderId_ShouldBeInvalid()
        => new MarkIfoodArrivedAtDestinationCommandValidator().Validate(new MarkIfoodArrivedAtDestinationCommand(0)).IsValid.Should().BeFalse();

    [Fact]
    public void MarkIfoodArrivedAtOriginCommandValidator_WithValidCommand_ShouldBeValid()
        => new MarkIfoodArrivedAtOriginCommandValidator().Validate(new MarkIfoodArrivedAtOriginCommand(1)).IsValid.Should().BeTrue();

    [Fact]
    public void MarkIfoodArrivedAtOriginCommandValidator_WithZeroOrderId_ShouldBeInvalid()
        => new MarkIfoodArrivedAtOriginCommandValidator().Validate(new MarkIfoodArrivedAtOriginCommand(0)).IsValid.Should().BeFalse();

    [Fact]
    public void MarkIfoodGoingToOriginCommandValidator_WithValidCommand_ShouldBeValid()
        => new MarkIfoodGoingToOriginCommandValidator().Validate(new MarkIfoodGoingToOriginCommand(1)).IsValid.Should().BeTrue();

    [Fact]
    public void MarkIfoodGoingToOriginCommandValidator_WithZeroOrderId_ShouldBeInvalid()
        => new MarkIfoodGoingToOriginCommandValidator().Validate(new MarkIfoodGoingToOriginCommand(0)).IsValid.Should().BeFalse();

    [Fact]
    public void VerifyIfoodDeliveryCodeCommandValidator_WithValidCommand_ShouldBeValid()
        => new VerifyIfoodDeliveryCodeCommandValidator().Validate(new VerifyIfoodDeliveryCodeCommand(1, "1234")).IsValid.Should().BeTrue();

    [Fact]
    public void VerifyIfoodDeliveryCodeCommandValidator_WithZeroOrderId_ShouldBeInvalid()
        => new VerifyIfoodDeliveryCodeCommandValidator().Validate(new VerifyIfoodDeliveryCodeCommand(0, "1234")).IsValid.Should().BeFalse();

    [Fact]
    public void VerifyIfoodDeliveryCodeCommandValidator_WithEmptyCode_ShouldBeInvalid()
        => new VerifyIfoodDeliveryCodeCommandValidator().Validate(new VerifyIfoodDeliveryCodeCommand(1, "")).IsValid.Should().BeFalse();

    [Fact]
    public void VerifyIfoodDeliveryCodeCommandValidator_WithCodeLongerThanTwentyChars_ShouldBeInvalid()
        => new VerifyIfoodDeliveryCodeCommandValidator()
            .Validate(new VerifyIfoodDeliveryCodeCommand(1, new string('9', 21)))
            .IsValid.Should().BeFalse();
}
