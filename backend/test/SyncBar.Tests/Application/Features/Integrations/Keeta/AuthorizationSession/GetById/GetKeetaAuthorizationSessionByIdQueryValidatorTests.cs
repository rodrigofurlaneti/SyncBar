using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetById;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.AuthorizationSession.GetById;

public sealed class GetKeetaAuthorizationSessionByIdQueryValidatorTests
{
    private readonly GetKeetaAuthorizationSessionByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetKeetaAuthorizationSessionByIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetKeetaAuthorizationSessionByIdQuery(id)).IsValid.Should().BeFalse();
}
