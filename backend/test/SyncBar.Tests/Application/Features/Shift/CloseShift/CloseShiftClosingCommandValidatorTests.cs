using FluentAssertions;
using SyncBar.Application.Features.Shift.CloseShift;
using Xunit;

namespace SyncBar.Tests.Application.Features.Shift.CloseShift;

public sealed class CloseShiftClosingCommandValidatorTests
{
    private readonly CloseShiftClosingCommandValidator _validator = new();

    private static CloseShiftClosingCommand Valid() => new(1, 1, null);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveShiftClosingId_ShouldBeInvalid(long shiftClosingId)
        => _validator.Validate(Valid() with { ShiftClosingId = shiftClosingId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveClosedByEmployeeId_ShouldBeInvalid(long closedByEmployeeId)
        => _validator.Validate(Valid() with { ClosedByEmployeeId = closedByEmployeeId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongNotes_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Notes = new string('a', 501) }).IsValid.Should().BeFalse();
}
