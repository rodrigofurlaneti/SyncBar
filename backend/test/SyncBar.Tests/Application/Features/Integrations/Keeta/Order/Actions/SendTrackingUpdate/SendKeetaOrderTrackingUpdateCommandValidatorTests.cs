using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.SendTrackingUpdate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.SendTrackingUpdate;

public sealed class SendKeetaOrderTrackingUpdateCommandValidatorTests
{
    private readonly SendKeetaOrderTrackingUpdateCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new SendKeetaOrderTrackingUpdateCommand(1, "DISPATCHED")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveOrderId_ShouldBeInvalid(long orderId)
        => _validator.Validate(new SendKeetaOrderTrackingUpdateCommand(orderId, "DISPATCHED")).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyTrackingEventType_ShouldBeInvalid()
        => _validator.Validate(new SendKeetaOrderTrackingUpdateCommand(1, string.Empty)).IsValid.Should().BeFalse();
}
