using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.AuthorizationSession.Create;

public sealed class CreateKeetaIntegrationAuthorizationSessionCommandValidatorTests
{
    private readonly CreateKeetaIntegrationAuthorizationSessionCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new CreateKeetaIntegrationAuthorizationSessionCommand(1, 1, "auth-1", 1))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new CreateKeetaIntegrationAuthorizationSessionCommand(companyId, 1, "auth-1", 1))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(new CreateKeetaIntegrationAuthorizationSessionCommand(1, branchId, "auth-1", 1))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyAuthId_ShouldBeInvalid()
        => _validator.Validate(new CreateKeetaIntegrationAuthorizationSessionCommand(1, 1, string.Empty, 1))
            .IsValid.Should().BeFalse();
}
