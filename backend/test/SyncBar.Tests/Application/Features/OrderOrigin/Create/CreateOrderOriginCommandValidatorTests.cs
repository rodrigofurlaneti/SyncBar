using FluentAssertions;
using SyncBar.Application.Features.OrderOrigin.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.OrderOrigin.Create;

public sealed class CreateOrderOriginCommandValidatorTests
{
    private readonly CreateOrderOriginCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new CreateOrderOriginCommand(1, 1, "Marketplace")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceName_ShouldBeInvalid(string? name)
        => _validator.Validate(new CreateOrderOriginCommand(1, 1, name!)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NameLongerThan100Chars_ShouldBeInvalid()
        => _validator.Validate(new CreateOrderOriginCommand(1, 1, new string('a', 101))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NameWithExactly100Chars_ShouldBeValid()
        => _validator.Validate(new CreateOrderOriginCommand(1, 1, new string('a', 100))).IsValid.Should().BeTrue();
}
