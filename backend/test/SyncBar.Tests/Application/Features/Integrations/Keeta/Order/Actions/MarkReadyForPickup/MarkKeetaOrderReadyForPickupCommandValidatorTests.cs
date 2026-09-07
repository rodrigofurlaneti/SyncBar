using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkReadyForPickup;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.MarkReadyForPickup;

public sealed class MarkKeetaOrderReadyForPickupCommandValidatorTests
{
    private readonly MarkKeetaOrderReadyForPickupCommandValidator _validator = new();

    [Fact]
    public void Validate_PositiveOrderId_ShouldBeValid()
        => _validator.Validate(new MarkKeetaOrderReadyForPickupCommand(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveOrderId_ShouldBeInvalid(long orderId)
        => _validator.Validate(new MarkKeetaOrderReadyForPickupCommand(orderId)).IsValid.Should().BeFalse();
}
