using FluentAssertions;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.ExistsByEventId;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.ExistsByEventId;

public sealed class ExistsKeetaOrderEventLogByEventIdQueryValidatorTests
{
    private readonly ExistsKeetaOrderEventLogByEventIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyEventId_ShouldBeValid()
        => _validator.Validate(new ExistsKeetaOrderEventLogByEventIdQuery("event-1")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyEventId_ShouldBeInvalid()
        => _validator.Validate(new ExistsKeetaOrderEventLogByEventIdQuery(string.Empty)).IsValid.Should().BeFalse();
}
