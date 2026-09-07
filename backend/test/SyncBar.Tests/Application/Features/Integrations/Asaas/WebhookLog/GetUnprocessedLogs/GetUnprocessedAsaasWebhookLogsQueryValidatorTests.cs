using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetUnprocessedLogs;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.GetUnprocessedLogs;

public sealed class GetUnprocessedAsaasWebhookLogsQueryValidatorTests
{
    private readonly GetUnprocessedAsaasWebhookLogsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQueryWithDefaultLimit_ShouldBeValid()
        => _validator.Validate(new GetUnprocessedAsaasWebhookLogsQuery(1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new GetUnprocessedAsaasWebhookLogsQuery(companyId)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void Validate_LimitOutOfRange_ShouldBeInvalid(int limit)
        => _validator.Validate(new GetUnprocessedAsaasWebhookLogsQuery(1, limit)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public void Validate_LimitAtBoundaries_ShouldBeValid(int limit)
        => _validator.Validate(new GetUnprocessedAsaasWebhookLogsQuery(1, limit)).IsValid.Should().BeTrue();
}
