using FluentAssertions;
using SyncBar.Application.Features.Dining.Table.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Table.Create;

public sealed class CreateDiningAreaTableCommandValidatorTests
{
    private readonly CreateDiningAreaTableCommandValidator _validator = new();

    private static CreateDiningAreaTableCommand Valid() => new(1, 1);

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
    public void Validate_NonPositiveDiningTableId_ShouldBeInvalid(long diningTableId)
        => _validator.Validate(Valid() with { DiningTableId = diningTableId }).IsValid.Should().BeFalse();
}
