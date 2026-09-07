using FluentAssertions;
using SyncBar.Application.Features.Dining.Assignment.End;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Assignment.End;

public sealed class EndDiningAreaAssignmentCommandValidatorTests
{
    private readonly EndDiningAreaAssignmentCommandValidator _validator = new();

    private static EndDiningAreaAssignmentCommand Valid() => new(1, DateTime.UtcNow);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(Valid() with { Id = id }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyEndAt_ShouldBeInvalid()
        => _validator.Validate(Valid() with { EndAt = default }).IsValid.Should().BeFalse();
}
