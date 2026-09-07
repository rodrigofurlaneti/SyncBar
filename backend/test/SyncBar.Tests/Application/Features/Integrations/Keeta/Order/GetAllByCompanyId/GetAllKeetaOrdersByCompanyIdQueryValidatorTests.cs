using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetAllByCompanyId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.GetAllByCompanyId;

public sealed class GetAllKeetaOrdersByCompanyIdQueryValidatorTests
{
    private readonly GetAllKeetaOrdersByCompanyIdQueryValidator _validator = new();

    [Fact]
    public void Validate_PositiveCompanyId_ShouldBeValid()
        => _validator.Validate(new GetAllKeetaOrdersByCompanyIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetAllKeetaOrdersByCompanyIdQuery(companyId)).IsValid.Should().BeFalse();
}
