using FluentAssertions;
using SyncBar.Application.Features.Users.CreateRole;
using Xunit;

namespace SyncBar.Tests.Application.Features.Users.CreateRole;

public sealed class CreateRoleCommandValidatorTests
{
    private readonly CreateRoleCommandValidator _validator = new();

    private static CreateRoleCommand Valid() => new(1, "Manager", null);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(Valid() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Name = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongName_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Name = new string('a', 101) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_OverlongDescription_ShouldBeInvalid()
        => _validator.Validate(Valid() with { Description = new string('a', 301) }).IsValid.Should().BeFalse();
}
