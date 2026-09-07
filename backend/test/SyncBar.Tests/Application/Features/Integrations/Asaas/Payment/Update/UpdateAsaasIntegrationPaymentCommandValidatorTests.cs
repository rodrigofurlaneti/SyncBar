using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Update;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.Update;

public sealed class UpdateAsaasIntegrationPaymentCommandValidatorTests
{
    private readonly UpdateAsaasIntegrationPaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommandWithoutNetValue_ShouldBeValid()
        => _validator.Validate(new UpdateAsaasIntegrationPaymentCommand(1, "PENDING")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidCommandWithNonNegativeNetValue_ShouldBeValid()
        => _validator.Validate(new UpdateAsaasIntegrationPaymentCommand(1, "RECEIVED", NetValue: 10m))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new UpdateAsaasIntegrationPaymentCommand(id, "PENDING")).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyStatus_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasIntegrationPaymentCommand(1, string.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_UnrecognizedStatus_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasIntegrationPaymentCommand(1, "INVALID_STATUS")).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeNetValue_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasIntegrationPaymentCommand(1, "PENDING", NetValue: -1m))
            .IsValid.Should().BeFalse();
}
