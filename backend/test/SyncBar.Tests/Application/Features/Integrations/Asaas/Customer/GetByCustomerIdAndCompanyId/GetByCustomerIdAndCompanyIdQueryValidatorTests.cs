using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.GetByCustomerIdAndCompanyId;

public sealed class GetByCustomerIdAndCompanyIdQueryValidatorTests
{
    private readonly GetByCustomerIdAndCompanyIdQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_ShouldBeValid()
        => _validator.Validate(new GetByCustomerIdAndCompanyIdQuery(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerId_ShouldBeInvalid(long customerId)
        => _validator.Validate(new GetByCustomerIdAndCompanyIdQuery(customerId, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetByCustomerIdAndCompanyIdQuery(1, companyId)).IsValid.Should().BeFalse();
}
