using FluentAssertions;
using SyncBar.Application.Features.CustomerAddresses.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAddresses.Create;

public sealed class CreateCustomerAddressCommandValidatorTests
{
    private readonly CreateCustomerAddressCommandValidator _validator = new();

    private static CreateCustomerAddressCommand Valid() => new(
        1,
        null,
        null,
        "Main Street",
        "123",
        "Apt 1",
        "12345-678");

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(Valid() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyStreet_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Street = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongStreet_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Street = new string('a', 501) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyNumber_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Number = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongNumber_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Number = new string('1', 51) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongSupplement_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Supplement = new string('a', 51) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongZipCode_ShouldBeInvalid()
        => _validator.Validate(Valid() with { ZipCode = new string('1', 10) }).IsValid.Should().BeFalse();
}
