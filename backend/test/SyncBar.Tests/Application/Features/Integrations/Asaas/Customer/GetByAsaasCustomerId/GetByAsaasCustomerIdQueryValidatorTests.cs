using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Customer.GetByAsaasCustomerId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.GetByAsaasCustomerId;

public sealed class GetByAsaasCustomerIdQueryValidatorTests
{
    private readonly GetByAsaasCustomerIdQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidAsaasCustomerId_ShouldBeValid()
        => _validator.Validate(new GetByAsaasCustomerIdQuery("cus_000001")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyAsaasCustomerId_ShouldBeInvalid()
        => _validator.Validate(new GetByAsaasCustomerIdQuery(string.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasCustomerIdExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new GetByAsaasCustomerIdQuery(new string('a', 51))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasCustomerIdAtMaxLength_ShouldBeValid()
        => _validator.Validate(new GetByAsaasCustomerIdQuery(new string('a', 50))).IsValid.Should().BeTrue();
}
