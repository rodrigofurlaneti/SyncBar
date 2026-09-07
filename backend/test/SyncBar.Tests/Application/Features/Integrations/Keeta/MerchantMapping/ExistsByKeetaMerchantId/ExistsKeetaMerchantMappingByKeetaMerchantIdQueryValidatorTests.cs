using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.ExistsByKeetaMerchantId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.MerchantMapping.ExistsByKeetaMerchantId;

public sealed class ExistsKeetaMerchantMappingByKeetaMerchantIdQueryValidatorTests
{
    private readonly ExistsKeetaMerchantMappingByKeetaMerchantIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveKeetaMerchantId_ShouldBeValid()
        => _validator.Validate(new ExistsKeetaMerchantMappingByKeetaMerchantIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveKeetaMerchantId_ShouldBeInvalid(long keetaMerchantId)
        => _validator.Validate(new ExistsKeetaMerchantMappingByKeetaMerchantIdQuery(keetaMerchantId)).IsValid.Should().BeFalse();
}
