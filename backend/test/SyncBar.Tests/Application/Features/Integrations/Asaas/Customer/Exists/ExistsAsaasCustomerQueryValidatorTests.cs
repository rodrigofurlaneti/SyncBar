using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Exists;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.Exists;

public sealed class ExistsAsaasCustomerQueryValidatorTests
{
    private readonly ExistsAsaasCustomerQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_ShouldBeValid()
        => _validator.Validate(new ExistsAsaasCustomerQuery(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerId_ShouldBeInvalid(long customerId)
        => _validator.Validate(new ExistsAsaasCustomerQuery(customerId, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new ExistsAsaasCustomerQuery(1, companyId)).IsValid.Should().BeFalse();
}
