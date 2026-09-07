using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.Payment.ExistsByAsaasPaymentId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.Payment.ExistsByAsaasPaymentId;

public sealed class ExistsByAsaasPaymentIdQueryValidatorTests
{
    private readonly ExistsByAsaasPaymentIdQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidAsaasPaymentId_ShouldBeValid()
        => _validator.Validate(new ExistsByAsaasPaymentIdQuery("pay_000001")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyAsaasPaymentId_ShouldBeInvalid()
        => _validator.Validate(new ExistsByAsaasPaymentIdQuery(string.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasPaymentIdExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new ExistsByAsaasPaymentIdQuery(new string('a', 51))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasPaymentIdAtMaxLength_ShouldBeValid()
        => _validator.Validate(new ExistsByAsaasPaymentIdQuery(new string('a', 50))).IsValid.Should().BeTrue();
}
