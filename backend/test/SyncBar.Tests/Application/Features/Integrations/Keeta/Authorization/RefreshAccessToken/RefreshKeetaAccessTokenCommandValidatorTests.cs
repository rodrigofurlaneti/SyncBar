using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.RefreshAccessToken;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Authorization.RefreshAccessToken;

public sealed class RefreshKeetaAccessTokenCommandValidatorTests
{
    private readonly RefreshKeetaAccessTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new RefreshKeetaAccessTokenCommand(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new RefreshKeetaAccessTokenCommand(companyId, 1)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeBranchId_ShouldBeInvalid()
        => _validator.Validate(new RefreshKeetaAccessTokenCommand(1, -1)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ZeroBranchId_ShouldBeValid()
        => _validator.Validate(new RefreshKeetaAccessTokenCommand(1, 0)).IsValid.Should().BeTrue();
}
