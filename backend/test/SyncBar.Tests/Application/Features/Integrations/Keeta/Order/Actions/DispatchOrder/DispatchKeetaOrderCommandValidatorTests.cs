using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.DispatchOrder;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.DispatchOrder;

public sealed class DispatchKeetaOrderCommandValidatorTests
{
    private readonly DispatchKeetaOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_PositiveOrderId_ShouldBeValid()
        => _validator.Validate(new DispatchKeetaOrderCommand(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveOrderId_ShouldBeInvalid(long orderId)
        => _validator.Validate(new DispatchKeetaOrderCommand(orderId)).IsValid.Should().BeFalse();
}
