using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.Create;

public sealed class CreateAsaasWebhookLogCommandValidatorTests
{
    private readonly CreateAsaasWebhookLogCommandValidator _validator = new();

    private static CreateAsaasWebhookLogCommand ValidCommand()
        => new(1, null, "PAYMENT_RECEIVED", null, null, "{}", null, null);

    [Fact]
    public void Validate_ValidCommandWithoutOptionalFields_ShouldBeValid()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidCommandWithAllOptionalFields_ShouldBeValid()
        => _validator.Validate(ValidCommand() with
        {
            BranchId = 1,
            AsaasEventId = "evt_000001",
            PaymentId = "pay_000001",
            RequestHeaders = "{\"content-type\":\"application/json\"}",
            IpAddress = "127.0.0.1"
        }).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(ValidCommand() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchIdWhenProvided_ShouldBeInvalid(long branchId)
        => _validator.Validate(ValidCommand() with { BranchId = branchId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyEvent_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { Event = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EventExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { Event = new string('a', 101) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasEventIdExceedingMaxLengthWhenProvided_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { AsaasEventId = new string('a', 151) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_PaymentIdExceedingMaxLengthWhenProvided_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { PaymentId = new string('a', 101) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyPayload_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { Payload = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_IpAddressExceedingMaxLengthWhenProvided_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { IpAddress = new string('1', 46) }).IsValid.Should().BeFalse();
}
