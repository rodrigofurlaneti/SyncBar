using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Update;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.RefundDispute.Update;

public sealed class UpdateKeetaIntegrationRefundDisputeCommandValidatorTests
{
    private readonly UpdateKeetaIntegrationRefundDisputeCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new UpdateKeetaIntegrationRefundDisputeCommand(1, 1, true)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new UpdateKeetaIntegrationRefundDisputeCommand(id, 1, true)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new UpdateKeetaIntegrationRefundDisputeCommand(1, companyId, true)).IsValid.Should().BeFalse();
}
