using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetById;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.GetById;

public sealed class GetAsaasSavedCardByIdQueryValidatorTests
{
    private readonly GetAsaasSavedCardByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetAsaasSavedCardByIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetAsaasSavedCardByIdQuery(id)).IsValid.Should().BeFalse();
}
