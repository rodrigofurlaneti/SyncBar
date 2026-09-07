using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Customer.Update;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Customer.Update;

public sealed class UpdateAsaasIntegrationCustomerCommandValidatorTests
{
    private readonly UpdateAsaasIntegrationCustomerCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new UpdateAsaasIntegrationCustomerCommand(1, "cus_000001"))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new UpdateAsaasIntegrationCustomerCommand(id, "cus_000001"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyNewAsaasCustomerId_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasIntegrationCustomerCommand(1, string.Empty))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NewAsaasCustomerIdExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasIntegrationCustomerCommand(1, new string('a', 51)))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NewAsaasCustomerIdAtMaxLength_ShouldBeValid()
        => _validator.Validate(new UpdateAsaasIntegrationCustomerCommand(1, new string('a', 50)))
            .IsValid.Should().BeTrue();
}
