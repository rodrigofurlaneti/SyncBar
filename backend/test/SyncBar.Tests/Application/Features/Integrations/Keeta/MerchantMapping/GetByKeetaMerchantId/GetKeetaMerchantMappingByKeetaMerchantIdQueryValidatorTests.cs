using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetByKeetaMerchantId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.GetByKeetaMerchantId;

public sealed class GetKeetaMerchantMappingByKeetaMerchantIdQueryValidatorTests
{
    private readonly GetKeetaMerchantMappingByKeetaMerchantIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveKeetaMerchantId_ShouldBeValid()
        => _validator.Validate(new GetKeetaMerchantMappingByKeetaMerchantIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveKeetaMerchantId_ShouldBeInvalid(long keetaMerchantId)
        => _validator.Validate(new GetKeetaMerchantMappingByKeetaMerchantIdQuery(keetaMerchantId)).IsValid.Should().BeFalse();
}
