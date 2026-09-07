using FluentAssertions;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.HasAlreadyProcessedEvent;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Asaas.WebhookLog.HasAlreadyProcessedEvent;

public sealed class HasAlreadyProcessedEventQueryValidatorTests
{
    private readonly HasAlreadyProcessedEventQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidAsaasEventId_ShouldBeValid()
        => _validator.Validate(new HasAlreadyProcessedEventQuery("evt_000001")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyAsaasEventId_ShouldBeInvalid()
        => _validator.Validate(new HasAlreadyProcessedEventQuery(string.Empty)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasEventIdExceedingMaxLength_ShouldBeInvalid()
        => _validator.Validate(new HasAlreadyProcessedEventQuery(new string('a', 151))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_AsaasEventIdAtMaxLength_ShouldBeValid()
        => _validator.Validate(new HasAlreadyProcessedEventQuery(new string('a', 150))).IsValid.Should().BeTrue();
}
