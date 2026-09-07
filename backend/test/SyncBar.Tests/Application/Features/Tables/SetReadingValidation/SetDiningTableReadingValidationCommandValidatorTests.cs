using FluentAssertions;
using SyncBar.Application.Features.Tables.SetReadingValidation;
using Xunit;

namespace SyncBar.Tests.Application.Features.Tables.SetReadingValidation;

public sealed class SetDiningTableReadingValidationCommandValidatorTests
{
    private readonly SetDiningTableReadingValidationCommandValidator _validator = new();

    private static SetDiningTableReadingValidationCommand Valid() => new(1, true, false, true);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveDiningTableId_ShouldBeInvalid(long diningTableId)
        => _validator.Validate(Valid() with { DiningTableId = diningTableId }).IsValid.Should().BeFalse();
}
