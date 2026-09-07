using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.ConfirmOrder;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.ConfirmOrder;

public sealed class ConfirmKeetaOrderCommandValidatorTests
{
    private readonly ConfirmKeetaOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommandWithoutPreparationTime_ShouldBeValid()
        => _validator.Validate(new ConfirmKeetaOrderCommand(1)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidCommandWithPreparationTime_ShouldBeValid()
        => _validator.Validate(new ConfirmKeetaOrderCommand(1, "reason", 10)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveOrderId_ShouldBeInvalid(long orderId)
        => _validator.Validate(new ConfirmKeetaOrderCommand(orderId)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositivePreparationTime_WhenProvided_ShouldBeInvalid(int preparationTime)
        => _validator.Validate(new ConfirmKeetaOrderCommand(1, null, preparationTime)).IsValid.Should().BeFalse();
}
