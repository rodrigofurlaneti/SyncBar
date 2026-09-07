using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetByAuthId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.AuthorizationSession.GetByAuthId;

public sealed class GetKeetaAuthorizationSessionByAuthIdQueryValidatorTests
{
    private readonly GetKeetaAuthorizationSessionByAuthIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyAuthId_ShouldBeValid()
        => _validator.Validate(new GetKeetaAuthorizationSessionByAuthIdQuery("auth-1"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyAuthId_ShouldBeInvalid()
        => _validator.Validate(new GetKeetaAuthorizationSessionByAuthIdQuery(string.Empty))
            .IsValid.Should().BeFalse();
}
