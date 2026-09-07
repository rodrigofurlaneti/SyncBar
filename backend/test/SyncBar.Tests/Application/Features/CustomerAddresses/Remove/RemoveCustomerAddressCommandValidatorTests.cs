using FluentAssertions;
using SyncBar.Application.Features.CustomerAddresses.Remove;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.Remove;

public sealed class RemoveCustomerAddressCommandValidatorTests
{
    private readonly RemoveCustomerAddressCommandValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new RemoveCustomerAddressCommand(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new RemoveCustomerAddressCommand(id)).IsValid.Should().BeFalse();
}
