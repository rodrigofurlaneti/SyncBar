using FluentAssertions;
using SyncBar.Application.Features.Shift.OpenShift;
using Xunit;

namespace SyncBar.Tests.Application.Features.Shift.OpenShift;

public sealed class OpenShiftClosingCommandValidatorTests
{
    private readonly OpenShiftClosingCommandValidator _validator = new();

    private static OpenShiftClosingCommand Valid() => new(1, 1);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(Valid() with { BranchId = branchId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveOpenedByEmployeeId_ShouldBeInvalid(long openedByEmployeeId)
        => _validator.Validate(Valid() with { OpenedByEmployeeId = openedByEmployeeId }).IsValid.Should().BeFalse();
}
