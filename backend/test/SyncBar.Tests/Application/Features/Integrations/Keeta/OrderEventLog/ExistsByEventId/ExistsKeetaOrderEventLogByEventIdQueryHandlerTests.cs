using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.ExistsByEventId;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.ExistsByEventId;

public sealed class ExistsKeetaOrderEventLogByEventIdQueryHandlerTests
{
    private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository = Substitute.For<IKeetaIntegrationOrderEventLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ExistsKeetaOrderEventLogByEventIdQueryHandler _handler;

    public ExistsKeetaOrderEventLogByEventIdQueryHandlerTests()
    {
        _handler = new ExistsKeetaOrderEventLogByEventIdQueryHandler(_eventLogRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_EventLogExists_ShouldReturnTrue()
    {
        _eventLogRepository.ExistsByEventIdAsync("event-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(new ExistsKeetaOrderEventLogByEventIdQuery("event-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EventLogDoesNotExist_ShouldReturnFalse()
    {
        _eventLogRepository.ExistsByEventIdAsync("event-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(new ExistsKeetaOrderEventLogByEventIdQuery("event-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }
}
