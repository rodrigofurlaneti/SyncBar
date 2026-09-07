using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Delete;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.Delete;

public sealed class DeleteAsaasWebhookLogCommandValidatorTests
{
    private readonly DeleteAsaasWebhookLogCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new DeleteAsaasWebhookLogCommand(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new DeleteAsaasWebhookLogCommand(id, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new DeleteAsaasWebhookLogCommand(1, companyId)).IsValid.Should().BeFalse();
}
