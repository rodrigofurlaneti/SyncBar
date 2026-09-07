using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetByEventId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.GetByEventId;

public sealed class GetKeetaOrderEventLogByEventIdQueryValidatorTests
{
    private readonly GetKeetaOrderEventLogByEventIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyEventId_ShouldBeValid()
        => _validator.Validate(new GetKeetaOrderEventLogByEventIdQuery("event-1")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyEventId_ShouldBeInvalid()
        => _validator.Validate(new GetKeetaOrderEventLogByEventIdQuery(string.Empty)).IsValid.Should().BeFalse();
}
