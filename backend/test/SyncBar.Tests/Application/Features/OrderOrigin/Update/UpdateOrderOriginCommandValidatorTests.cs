using FluentAssertions;
using SyncBar.Application.Features.OrderOrigin.Update;
using Xunit;

namespace SyncBar.Tests.Application.Features.OrderOrigin.Update;

public sealed class UpdateOrderOriginCommandValidatorTests
{
    private readonly UpdateOrderOriginCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new UpdateOrderOriginCommand(1, 1, 1, "Marketplace", true)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new UpdateOrderOriginCommand(id, 1, 1, "Marketplace", true)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceName_ShouldBeInvalid(string? name)
        => _validator.Validate(new UpdateOrderOriginCommand(1, 1, 1, name!, true)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NameLongerThan100Chars_ShouldBeInvalid()
        => _validator.Validate(new UpdateOrderOriginCommand(1, 1, 1, new string('a', 101), true)).IsValid.Should().BeFalse();
}
