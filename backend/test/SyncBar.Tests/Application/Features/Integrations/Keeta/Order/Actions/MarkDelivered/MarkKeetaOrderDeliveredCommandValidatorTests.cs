using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkDelivered;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.MarkDelivered;

public sealed class MarkKeetaOrderDeliveredCommandValidatorTests
{
    private readonly MarkKeetaOrderDeliveredCommandValidator _validator = new();

    [Fact]
    public void Validate_PositiveOrderId_ShouldBeValid()
        => _validator.Validate(new MarkKeetaOrderDeliveredCommand(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveOrderId_ShouldBeInvalid(long orderId)
        => _validator.Validate(new MarkKeetaOrderDeliveredCommand(orderId)).IsValid.Should().BeFalse();
}
