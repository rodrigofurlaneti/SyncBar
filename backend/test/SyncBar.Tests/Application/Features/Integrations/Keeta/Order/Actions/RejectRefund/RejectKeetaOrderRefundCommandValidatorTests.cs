using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RejectRefund;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Actions.RejectRefund;

public sealed class RejectKeetaOrderRefundCommandValidatorTests
{
    private readonly RejectKeetaOrderRefundCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new RejectKeetaOrderRefundCommand(1, "reason", "OTHER")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveOrderId_ShouldBeInvalid(long orderId)
        => _validator.Validate(new RejectKeetaOrderRefundCommand(orderId, "reason", "OTHER")).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyReason_ShouldBeInvalid()
        => _validator.Validate(new RejectKeetaOrderRefundCommand(1, string.Empty, "OTHER")).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_InvalidCode_ShouldBeInvalid()
        => _validator.Validate(new RejectKeetaOrderRefundCommand(1, "reason", "INVALID_CODE")).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("DISH_ALREADY_DONE")]
    [InlineData("OUT_FOR_DELIVERY")]
    [InlineData("OTHER")]
    public void Validate_ValidCodes_ShouldBeValid(string code)
        => _validator.Validate(new RejectKeetaOrderRefundCommand(1, "reason", code)).IsValid.Should().BeTrue();
}
