using FluentAssertions;
using SyncBar.Application.Features.Dining.Area.Update;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Area.Update;

public sealed class UpdateDiningAreaCommandValidatorTests
{
    private readonly UpdateDiningAreaCommandValidator _validator = new();

    private static UpdateDiningAreaCommand Valid() => new(1, "Salão Principal");

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(Valid() with { Id = id }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Name = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Name = new string('a', 101) }).IsValid.Should().BeFalse();
}
