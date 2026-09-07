using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.HandleOAuthCallback;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Authorization.HandleOAuthCallback;

public sealed class HandleKeetaOAuthCallbackCommandValidatorTests
{
    private readonly HandleKeetaOAuthCallbackCommandValidator _validator = new();

    private static HandleKeetaOAuthCallbackCommand ValidCommand() => new(
        CompanyId: 1,
        BranchId: 1,
        AuthId: "auth-1",
        State: "state",
        KeetaMerchantId: 1,
        Code: "code");

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
    public void Validate_EmptyAuthId_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { AuthId = string.Empty }).IsValid.Should().BeFalse();
}
