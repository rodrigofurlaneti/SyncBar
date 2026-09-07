using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Delete;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.Delete;

public sealed class DeleteAsaasCustomerCommandValidatorTests
{
    private readonly DeleteAsaasCustomerCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new DeleteAsaasCustomerCommand(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCustomerId_ShouldBeInvalid(long customerId)
        => _validator.Validate(new DeleteAsaasCustomerCommand(customerId, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new DeleteAsaasCustomerCommand(1, companyId)).IsValid.Should().BeFalse();
}
