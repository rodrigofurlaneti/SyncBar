using FluentAssertions;
using SyncBar.Application.Features.Users.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Users.Create;

public sealed class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    private static CreateUserCommand Valid() => new(
        1,
        1,
        "johndoe",
        "john@example.com",
        "password123",
        [1]);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(Valid() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveEmployeeId_ShouldBeInvalid(long employeeId)
        => _validator.Validate(Valid() with { EmployeeId = employeeId }).IsValid.Should().BeFalse();

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
        => _validator.Validate(Valid() with { Password = "1234567" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongPassword_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Password = new string('a', 201) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyRoleIds_ShouldBeInvalid()
        => _validator.Validate(Valid() with { RoleIds = [] }).IsValid.Should().BeFalse();
}

