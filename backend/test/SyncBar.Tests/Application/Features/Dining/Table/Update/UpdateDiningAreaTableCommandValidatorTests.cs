using FluentAssertions;
using SyncBar.Application.Features.Dining.Table.Update;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Table.Update;

public sealed class UpdateDiningAreaTableCommandValidatorTests
{
    private readonly UpdateDiningAreaTableCommandValidator _validator = new();

    private static UpdateDiningAreaTableCommand Valid() => new(1, 1, 1);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(Valid() with { Id = id }).IsValid.Should().BeFalse();

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
