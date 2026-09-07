using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId;

public sealed class GetSavedCardsByCustomerIdQueryValidatorTests
{
    private readonly GetSavedCardsByCustomerIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCustomerId_ShouldBeValid()
        => _validator.Validate(new GetSavedCardsByCustomerIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerId_ShouldBeInvalid(long customerId)
        => _validator.Validate(new GetSavedCardsByCustomerIdQuery(customerId)).IsValid.Should().BeFalse();
}
