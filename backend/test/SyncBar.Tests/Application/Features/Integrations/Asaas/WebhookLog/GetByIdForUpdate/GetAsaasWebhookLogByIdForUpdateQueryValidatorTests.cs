using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByIdForUpdate;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.GetByIdForUpdate;

public sealed class GetAsaasWebhookLogByIdForUpdateQueryValidatorTests
{
    private readonly GetAsaasWebhookLogByIdForUpdateQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_ShouldBeValid()
        => _validator.Validate(new GetAsaasWebhookLogByIdForUpdateQuery(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new GetAsaasWebhookLogByIdForUpdateQuery(id, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetAsaasWebhookLogByIdForUpdateQuery(1, companyId)).IsValid.Should().BeFalse();
}
