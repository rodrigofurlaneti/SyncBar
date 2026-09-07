using FluentAssertions;
using SyncBar.Application.Features.Dining.Table.Deactivate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Table.Deactivate;

public sealed class DeactivateDiningAreaTableCommandValidatorTests
{
    private readonly DeactivateDiningAreaTableCommandValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new DeactivateDiningAreaTableCommand(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new DeactivateDiningAreaTableCommand(id)).IsValid.Should().BeFalse();
}
