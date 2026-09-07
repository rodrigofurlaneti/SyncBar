using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByIdForUpdate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.GetByIdForUpdate;

public sealed class GetAsaasSavedCardByIdForUpdateQueryValidatorTests
{
    private readonly GetAsaasSavedCardByIdForUpdateQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveId_ShouldBeValid()
        => _validator.Validate(new GetAsaasSavedCardByIdForUpdateQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetAsaasSavedCardByIdForUpdateQuery(id)).IsValid.Should().BeFalse();
}
