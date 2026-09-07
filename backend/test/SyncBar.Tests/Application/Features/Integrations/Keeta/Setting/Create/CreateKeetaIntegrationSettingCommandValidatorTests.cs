using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Setting.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Setting.Create;

public sealed class CreateKeetaIntegrationSettingCommandValidatorTests
{
    private readonly CreateKeetaIntegrationSettingCommandValidator _validator = new();

    private static CreateKeetaIntegrationSettingCommand ValidCommand() => new(
        CompanyId: 1,
        BranchId: 1,
        ClientId: "client-id",
        ClientSecret: "client-secret",
        AppId: "app-id");

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(ValidCommand() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeBranchId_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { BranchId = -1 }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ZeroBranchId_ShouldBeValid()
        => _validator.Validate(ValidCommand() with { BranchId = 0 }).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyClientId_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { ClientId = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyClientSecret_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { ClientSecret = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyAppId_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { AppId = string.Empty }).IsValid.Should().BeFalse();
}
