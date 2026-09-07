using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;

public sealed class GetAsaasWebhookLogsByPaymentIdQueryValidatorTests
{
    private readonly GetAsaasWebhookLogsByPaymentIdQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_ShouldBeValid()
        => _validator.Validate(new GetAsaasWebhookLogsByPaymentIdQuery(1, "pay_000001")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetAsaasWebhookLogsByPaymentIdQuery(companyId, "pay_000001")).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyPaymentId_ShouldBeInvalid()
        => _validator.Validate(new GetAsaasWebhookLogsByPaymentIdQuery(1, string.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_PaymentIdExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new GetAsaasWebhookLogsByPaymentIdQuery(1, new string('a', 101))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_PaymentIdAtMaxLength_ShouldBeValid()
        => _validator.Validate(new GetAsaasWebhookLogsByPaymentIdQuery(1, new string('a', 100))).IsValid.Should().BeTrue();
}
