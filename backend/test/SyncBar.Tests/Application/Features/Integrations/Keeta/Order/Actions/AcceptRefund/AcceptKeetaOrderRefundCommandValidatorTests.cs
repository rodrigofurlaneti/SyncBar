using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.AcceptRefund;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.AcceptRefund;

public sealed class AcceptKeetaOrderRefundCommandValidatorTests
{
    private readonly AcceptKeetaOrderRefundCommandValidator _validator = new();

    [Fact]
    public void Validate_PositiveOrderId_ShouldBeValid()
        => _validator.Validate(new AcceptKeetaOrderRefundCommand(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveOrderId_ShouldBeInvalid(long orderId)
        => _validator.Validate(new AcceptKeetaOrderRefundCommand(orderId)).IsValid.Should().BeFalse();
}
