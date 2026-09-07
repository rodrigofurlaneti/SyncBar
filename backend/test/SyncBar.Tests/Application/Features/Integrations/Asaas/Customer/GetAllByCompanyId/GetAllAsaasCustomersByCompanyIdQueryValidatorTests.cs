using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.GetAllByCompanyId;

public sealed class GetAllAsaasCustomersByCompanyIdQueryValidatorTests
{
    private readonly GetAllAsaasCustomersByCompanyIdQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_ShouldBeValid()
        => _validator.Validate(new GetAllAsaasCustomersByCompanyIdQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetAllAsaasCustomersByCompanyIdQuery(companyId)).IsValid.Should().BeFalse();
}
