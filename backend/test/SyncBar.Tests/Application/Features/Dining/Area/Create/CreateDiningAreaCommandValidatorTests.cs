using FluentAssertions;
using SyncBar.Application.Features.Dining.Area.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Area.Create;

public sealed class CreateDiningAreaCommandValidatorTests
{
    private readonly CreateDiningAreaCommandValidator _validator = new();

    private static CreateDiningAreaCommand Valid() => new(1, "Salão Principal");

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(Valid() with { BranchId = branchId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Name = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Name = new string('a', 101) }).IsValid.Should().BeFalse();
}
