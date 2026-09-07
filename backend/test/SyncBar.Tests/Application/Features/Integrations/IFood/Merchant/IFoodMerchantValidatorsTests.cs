using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Merchant;

// Cobertura das regras de validação (FluentValidation) dos comandos de
// Integrations/Ifood/Merchant — sem FluentValidation.TestHelper (não referenciado neste projeto),
// então os asserts usam ValidationResult.IsValid/Errors diretamente.
public sealed class IfoodMerchantValidatorsTests
{
    private static readonly DateTime Start = new(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateIfoodInterruptionCommandValidator_WithValidCommand_ShouldBeValid()
        => new CreateIfoodInterruptionCommandValidator()
            .Validate(new CreateIfoodInterruptionCommand(1, "Manutenção", Start, Start.AddHours(1)))
            .IsValid.Should().BeTrue();

    [Fact]
    public void CreateIfoodInterruptionCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new CreateIfoodInterruptionCommandValidator()
            .Validate(new CreateIfoodInterruptionCommand(0, "Manutenção", Start, Start.AddHours(1)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodInterruptionCommandValidator_WithEmptyDescription_ShouldBeInvalid()
        => new CreateIfoodInterruptionCommandValidator()
            .Validate(new CreateIfoodInterruptionCommand(1, "", Start, Start.AddHours(1)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodInterruptionCommandValidator_WithDescriptionLongerThan255Chars_ShouldBeInvalid()
        => new CreateIfoodInterruptionCommandValidator()
            .Validate(new CreateIfoodInterruptionCommand(1, new string('A', 256), Start, Start.AddHours(1)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodInterruptionCommandValidator_WithEndBeforeStart_ShouldBeInvalid()
        => new CreateIfoodInterruptionCommandValidator()
            .Validate(new CreateIfoodInterruptionCommand(1, "Manutenção", Start, Start.AddMinutes(-1)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodInterruptionCommandValidator_WithDurationShorterThanOneMinute_ShouldBeInvalid()
        => new CreateIfoodInterruptionCommandValidator()
            .Validate(new CreateIfoodInterruptionCommand(1, "Manutenção", Start, Start.AddSeconds(30)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void CreateIfoodInterruptionCommandValidator_WithDurationLongerThanSevenDays_ShouldBeInvalid()
        => new CreateIfoodInterruptionCommandValidator()
            .Validate(new CreateIfoodInterruptionCommand(1, "Manutenção", Start, Start.AddDays(8)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SaveIfoodOpeningHoursCommandValidator_WithValidCommand_ShouldBeValid()
        => new SaveIfoodOpeningHoursCommandValidator()
            .Validate(new SaveIfoodOpeningHoursCommand(1, [new IfoodOpeningHourShiftInput(1, "08:00", 600)]))
            .IsValid.Should().BeTrue();

    [Fact]
    public void SaveIfoodOpeningHoursCommandValidator_WithZeroBranchId_ShouldBeInvalid()
        => new SaveIfoodOpeningHoursCommandValidator()
            .Validate(new SaveIfoodOpeningHoursCommand(0, [new IfoodOpeningHourShiftInput(1, "08:00", 600)]))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SaveIfoodOpeningHoursCommandValidator_WithDayOfWeekOutOfRange_ShouldBeInvalid()
        => new SaveIfoodOpeningHoursCommandValidator()
            .Validate(new SaveIfoodOpeningHoursCommand(1, [new IfoodOpeningHourShiftInput(7, "08:00", 600)]))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SaveIfoodOpeningHoursCommandValidator_WithEmptyStart_ShouldBeInvalid()
        => new SaveIfoodOpeningHoursCommandValidator()
            .Validate(new SaveIfoodOpeningHoursCommand(1, [new IfoodOpeningHourShiftInput(1, "", 600)]))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SaveIfoodOpeningHoursCommandValidator_WithInvalidStartFormat_ShouldBeInvalid()
        => new SaveIfoodOpeningHoursCommandValidator()
            .Validate(new SaveIfoodOpeningHoursCommand(1, [new IfoodOpeningHourShiftInput(1, "25:99", 600)]))
            .IsValid.Should().BeFalse();

    [Fact]
    public void SaveIfoodOpeningHoursCommandValidator_WithZeroDurationMinutes_ShouldBeInvalid()
        => new SaveIfoodOpeningHoursCommandValidator()
            .Validate(new SaveIfoodOpeningHoursCommand(1, [new IfoodOpeningHourShiftInput(1, "08:00", 0)]))
            .IsValid.Should().BeFalse();
}
