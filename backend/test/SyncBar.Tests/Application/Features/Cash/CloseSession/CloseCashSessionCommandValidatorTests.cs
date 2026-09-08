using FluentAssertions;
using SyncBar.Application.Features.Cash.CloseSession;
using Xunit;

namespace SyncBar.Tests.Application.Features.Cash.CloseSession;

public sealed class CloseCashSessionCommandValidatorTests
{
    private readonly CloseCashSessionCommandValidator _validator = new();

    private static CloseCashSessionCommand Valid() => new(CashSessionId: 1, ClosedByEmployeeId: 1, ClosingAmount: 100m);

    [Fact]
    public void Validate_ValidCommandWithoutPaymentMethodCounts_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidCommandWithPaymentMethodCounts_ShouldBeValid()
    {
        var command = Valid() with { PaymentMethodCounts = [new PaymentMethodCountRequest(2, 50m)] };

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCashSessionId_ShouldBeInvalid(long cashSessionId)
        => _validator.Validate(Valid() with { CashSessionId = cashSessionId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveClosedByEmployeeId_ShouldBeInvalid(long closedByEmployeeId)
        => _validator.Validate(Valid() with { ClosedByEmployeeId = closedByEmployeeId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeClosingAmount_ShouldBeInvalid()
        => _validator.Validate(Valid() with { ClosingAmount = -1m }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositivePaymentMethodIdInCounts_ShouldBeInvalid(long paymentMethodId)
    {
        var command = Valid() with { PaymentMethodCounts = [new PaymentMethodCountRequest(paymentMethodId, 50m)] };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeCountedAmountInCounts_ShouldBeInvalid()
    {
        var command = Valid() with { PaymentMethodCounts = [new PaymentMethodCountRequest(2, -1m)] };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
