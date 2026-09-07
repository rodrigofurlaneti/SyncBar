using FluentAssertions;
using SyncBar.Application.Features.Dining.Assignment.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Assignment.Create;

public sealed class CreateDiningAreaAssignmentCommandValidatorTests
{
    private readonly CreateDiningAreaAssignmentCommandValidator _validator = new();

    private static CreateDiningAreaAssignmentCommand Valid() => new(1, 1, DateTime.UtcNow);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveDiningAreaId_ShouldBeInvalid(long diningAreaId)
        => _validator.Validate(Valid() with { DiningAreaId = diningAreaId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveEmployeeId_ShouldBeInvalid(long employeeId)
        => _validator.Validate(Valid() with { EmployeeId = employeeId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyStartAt_ShouldBeInvalid()
        => _validator.Validate(Valid() with { StartAt = default }).IsValid.Should().BeFalse();
}
