using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetById;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.GetById;

public sealed class GetKeetaOrderByIdQueryValidatorTests
{
    private readonly GetKeetaOrderByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetKeetaOrderByIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetKeetaOrderByIdQuery(id)).IsValid.Should().BeFalse();
}
