using FluentAssertions;
using SyncBar.Application.Features.Employees.RegisterTeamMember;
using Xunit;

namespace SyncBar.Tests.Application.Features.Employees.RegisterTeamMember;

public sealed class RegisterTeamMemberCommandValidatorTests
{
    private readonly RegisterTeamMemberCommandValidator _validator = new();

    private static RegisterTeamMemberCommand Valid() => new(
        1,
        1,
        1,
        "John Doe",
        "12345678901",
        null,
        null,
        DateTime.UtcNow,
        null,
        false,
        null,
        null,
        null,
        null);

    private static RegisterTeamMemberCommand ValidWithSystemAccess() => Valid() with
    {
        HasSystemAccess = true,
        UserName = "johndoe",
        UserEmail = "john@example.com",
        Password = "password123",
    };

    [Fact]
    public void Validate_ValidCommandWithoutSystemAccess_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidCommandWithSystemAccess_ShouldBeValid()
        => _validator.Validate(ValidWithSystemAccess()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(Valid() with { BranchId = branchId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(Valid() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveJobTitleId_ShouldBeInvalid(long jobTitleId)
        => _validator.Validate(Valid() with { JobTitleId = jobTitleId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Name = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Name = new string('a', 151) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyCpf_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Cpf = string.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    public void Validate_WrongLengthCpf_ShouldBeInvalid(string cpf)
        => _validator.Validate(Valid() with { Cpf = cpf }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NonNumericCpf_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Cpf = "1234567890a" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_InvalidEmailFormat_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Email = "not-an-email" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongEmail_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Email = new string('a', 146) + "@a.co" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongPhone_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Phone = new string('1', 21) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeSalary_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Salary = -1m }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_HasSystemAccessWithoutUserName_ShouldBeInvalid()
        => _validator.Validate(ValidWithSystemAccess() with { UserName = null }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_HasSystemAccessWithOverlongUserName_ShouldBeInvalid()
        => _validator.Validate(ValidWithSystemAccess() with { UserName = new string('a', 101) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_HasSystemAccessWithoutUserEmail_ShouldBeInvalid()
        => _validator.Validate(ValidWithSystemAccess() with { UserEmail = null }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_HasSystemAccessWithInvalidUserEmail_ShouldBeInvalid()
        => _validator.Validate(ValidWithSystemAccess() with { UserEmail = "not-an-email" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_HasSystemAccessWithoutPassword_ShouldBeInvalid()
        => _validator.Validate(ValidWithSystemAccess() with { Password = null }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_HasSystemAccessWithShortPassword_ShouldBeInvalid()
        => _validator.Validate(ValidWithSystemAccess() with { Password = "1234567" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_HasSystemAccessWithOverlongPassword_ShouldBeInvalid()
        => _validator.Validate(ValidWithSystemAccess() with { Password = new string('a', 201) }).IsValid.Should().BeFalse();
}
