using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Update;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.Update;

public sealed class UpdateKeetaIntegrationOrderEventLogCommandValidatorTests
{
    private readonly UpdateKeetaIntegrationOrderEventLogCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(new UpdateKeetaIntegrationOrderEventLogCommand(1, 1)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveId_ShouldBeInvalid(long id)
        => _validator.Validate(new UpdateKeetaIntegrationOrderEventLogCommand(id, 1)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(new UpdateKeetaIntegrationOrderEventLogCommand(1, companyId)).IsValid.Should().BeFalse();
}
