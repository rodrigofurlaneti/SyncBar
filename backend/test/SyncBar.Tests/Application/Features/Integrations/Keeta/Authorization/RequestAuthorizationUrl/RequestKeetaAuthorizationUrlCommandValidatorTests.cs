using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.RequestAuthorizationUrl;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Authorization.RequestAuthorizationUrl;

public sealed class RequestKeetaAuthorizationUrlCommandValidatorTests
{
    private readonly RequestKeetaAuthorizationUrlCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new RequestKeetaAuthorizationUrlCommand(1, 1, "https://example.com/callback"))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new RequestKeetaAuthorizationUrlCommand(companyId, 1, "https://example.com/callback"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeBranchId_ShouldBeInvalid()
        => _validator.Validate(new RequestKeetaAuthorizationUrlCommand(1, -1, "https://example.com/callback"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ZeroBranchId_ShouldBeValid()
        => _validator.Validate(new RequestKeetaAuthorizationUrlCommand(1, 0, "https://example.com/callback"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyRedirectUri_ShouldBeInvalid()
        => _validator.Validate(new RequestKeetaAuthorizationUrlCommand(1, 1, string.Empty))
            .IsValid.Should().BeFalse();
}
