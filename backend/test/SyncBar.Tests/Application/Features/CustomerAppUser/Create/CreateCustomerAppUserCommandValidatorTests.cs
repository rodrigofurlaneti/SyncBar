using FluentAssertions;
using SyncBar.Application.Features.CustomerAppUser.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.CustomerAppUser.Create;

public sealed class CreateCustomerAppUserCommandValidatorTests
{
    private readonly CreateCustomerAppUserCommandValidator _validator = new();

    private static CreateCustomerAppUserCommand Valid() => new(
        1,
        null,
        null,
        "12345678900",
        "johndoe",
        "john@example.com",
        "password123");

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(Valid() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyUserName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { UserName = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongUserName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { UserName = new string('a', 101) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyEmail_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Email = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_InvalidEmailFormat_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Email = "not-an-email" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongEmail_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Email = new string('a', 146) + "@a.co" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyPassword_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Password = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ShortPassword_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Password = "12345" }).IsValid.Should().BeFalse();
}
