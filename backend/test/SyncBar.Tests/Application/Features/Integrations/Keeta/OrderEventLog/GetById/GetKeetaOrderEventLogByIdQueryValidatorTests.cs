using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.GetById;

public sealed class GetKeetaOrderEventLogByIdQueryValidatorTests
{
    private readonly GetKeetaOrderEventLogByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetKeetaOrderEventLogByIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetKeetaOrderEventLogByIdQuery(id)).IsValid.Should().BeFalse();
}
