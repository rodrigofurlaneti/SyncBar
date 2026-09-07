using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId;

public sealed class GetByAsaasPaymentIdQueryValidatorTests
{
    private readonly GetByAsaasPaymentIdQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidAsaasPaymentId_ShouldBeValid()
        => _validator.Validate(new GetByAsaasPaymentIdQuery("pay_000001")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyAsaasPaymentId_ShouldBeInvalid()
        => _validator.Validate(new GetByAsaasPaymentIdQuery(string.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasPaymentIdExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new GetByAsaasPaymentIdQuery(new string('a', 51))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasPaymentIdAtMaxLength_ShouldBeValid()
        => _validator.Validate(new GetByAsaasPaymentIdQuery(new string('a', 50))).IsValid.Should().BeTrue();
}
