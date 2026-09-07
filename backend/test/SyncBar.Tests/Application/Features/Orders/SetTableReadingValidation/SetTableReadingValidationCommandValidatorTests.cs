using FluentAssertions;
using SyncBar.Application.Features.Orders.SetTableReadingValidation;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.SetTableReadingValidation;

public sealed class SetTableReadingValidationCommandValidatorTests
{
    private readonly SetTableReadingValidationCommandValidator _validator = new();

    private static SetTableReadingValidationCommand Valid() => new(1, true, false, true);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(Valid() with { BranchId = branchId }).IsValid.Should().BeFalse();
}
