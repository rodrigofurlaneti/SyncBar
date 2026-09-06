using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetUnprocessedEvents;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.GetUnprocessedEvents;

public sealed class GetUnprocessedKeetaOrderEventLogsQueryHandlerTests
{
    private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository = Substitute.For<IKeetaIntegrationOrderEventLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetUnprocessedKeetaOrderEventLogsQueryHandler _handler;

    public GetUnprocessedKeetaOrderEventLogsQueryHandlerTests()
    {
        _handler = new GetUnprocessedKeetaOrderEventLogsQueryHandler(_eventLogRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_UnprocessedEventsExist_ShouldReturnMappedList()
    {
        var logs = new List<KeetaIntegrationOrderEventLog>
        {
            KeetaIntegrationOrderEventLog.Create(1, 2, "event-1", "order-1", "CREATED", "{}", DateTime.UtcNow).Value,
        };
        _eventLogRepository.GetUnprocessedEventsAsync(Arg.Any<CancellationToken>()).Returns(logs);

        var result = await _handler.Handle(new GetUnprocessedKeetaOrderEventLogsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_NoUnprocessedEvents_ShouldReturnEmptyList()
    {
        _eventLogRepository.GetUnprocessedEventsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetUnprocessedKeetaOrderEventLogsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
