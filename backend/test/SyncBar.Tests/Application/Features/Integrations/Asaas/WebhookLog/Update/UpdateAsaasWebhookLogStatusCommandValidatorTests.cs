using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Update;
using SyncBar.Domain.Enums;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.Update;

public sealed class UpdateAsaasWebhookLogStatusCommandValidatorTests
{
    private readonly UpdateAsaasWebhookLogStatusCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommandWithProcessedStatus_ShouldBeValid()
        => _validator.Validate(new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Processed))
            .IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ValidCommandWithFailedStatusAndErrorMessage_ShouldBeValid()
        => _validator.Validate(new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Failed, "boom"))
            .IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new UpdateAsaasWebhookLogStatusCommand(id, 1, WebhookLogStatus.Processed))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new UpdateAsaasWebhookLogStatusCommand(1, companyId, WebhookLogStatus.Processed))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_StatusNotProcessedOrFailed_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Pending))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_StatusOutsideEnumRange_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasWebhookLogStatusCommand(1, 1, (WebhookLogStatus)99))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_FailedStatusWithoutErrorMessage_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Failed))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Validate_FailedStatusWithErrorMessageExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new UpdateAsaasWebhookLogStatusCommand(1, 1, WebhookLogStatus.Failed, new string('a', 1001)))
            .IsValid.Should().BeFalse();
}
