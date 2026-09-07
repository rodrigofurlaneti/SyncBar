using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Create;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.Create;

public sealed class CreateKeetaIntegrationOrderEventLogCommandValidatorTests
{
    private readonly CreateKeetaIntegrationOrderEventLogCommandValidator _validator = new();

    private static CreateKeetaIntegrationOrderEventLogCommand ValidCommand() => new(
        CompanyId: 1,
        BranchId: 1,
        EventId: "event-1",
        OrderId: "order-1",
        EventType: "ORDER_CONFIRMED",
        RawPayload: "{}",
        EventCreatedAtUtc: DateTime.UtcNow);

    [Fact]
    public void Validate_ValidCommand_ShouldBeValid()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCompanyId_ShouldBeInvalid(long companyId)
        => _validator.Validate(ValidCommand() with { CompanyId = companyId }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveBranchId_ShouldBeInvalid(long branchId)
        => _validator.Validate(ValidCommand() with { BranchId = branchId }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyEventId_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { EventId = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyOrderId_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { OrderId = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyEventType_ShouldBeInvalid()
        => _validator.Validate(ValidCommand() with { EventType = string.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NullRawPayload_ShouldBeValid()
        => _validator.Validate(ValidCommand() with { RawPayload = null }).IsValid.Should().BeTrue();
}
