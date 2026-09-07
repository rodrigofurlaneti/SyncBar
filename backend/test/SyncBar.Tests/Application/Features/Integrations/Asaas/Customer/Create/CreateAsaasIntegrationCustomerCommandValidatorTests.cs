using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.Create;

public sealed class CreateAsaasIntegrationCustomerCommandValidatorTests
{
    private readonly CreateAsaasIntegrationCustomerCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new CreateAsaasIntegrationCustomerCommand(1, 1, "cus_000001"))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerId_ShouldBeInvalid(long customerId)
        => _validator.Validate(new CreateAsaasIntegrationCustomerCommand(customerId, 1, "cus_000001"))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new CreateAsaasIntegrationCustomerCommand(1, companyId, "cus_000001"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyAsaasCustomerId_ShouldBeInvalid()
        => _validator.Validate(new CreateAsaasIntegrationCustomerCommand(1, 1, string.Empty))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasCustomerIdExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new CreateAsaasIntegrationCustomerCommand(1, 1, new string('a', 51)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasCustomerIdAtMaxLength_ShouldBeValid()
        => _validator.Validate(new CreateAsaasIntegrationCustomerCommand(1, 1, new string('a', 50)))
            .IsValid.Should().BeTrue();
}
