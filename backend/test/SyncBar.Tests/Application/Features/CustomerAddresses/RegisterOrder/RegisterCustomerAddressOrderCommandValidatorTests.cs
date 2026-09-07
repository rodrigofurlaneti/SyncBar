using FluentAssertions;
using SyncBar.Application.Features.CustomerAddresses.RegisterOrder;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.RegisterOrder;

public sealed class RegisterCustomerAddressOrderCommandValidatorTests
{
    private readonly RegisterCustomerAddressOrderCommandValidator _validator = new();

    private static RegisterCustomerAddressOrderCommand Valid() => new(1, 1);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveAddressId_ShouldBeInvalid(long addressId)
        => _validator.Validate(Valid() with { AddressId = addressId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveOrderId_ShouldBeInvalid(long orderId)
        => _validator.Validate(Valid() with { OrderId = orderId }).IsValid.Should().BeFalse();
}
