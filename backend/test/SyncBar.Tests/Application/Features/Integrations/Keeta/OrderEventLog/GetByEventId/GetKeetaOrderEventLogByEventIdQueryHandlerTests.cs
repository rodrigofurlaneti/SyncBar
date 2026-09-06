using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetByEventId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.GetByEventId;

public sealed class GetKeetaOrderEventLogByEventIdQueryHandlerTests
{
    private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository = Substitute.For<IKeetaIntegrationOrderEventLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetKeetaOrderEventLogByEventIdQueryHandler _handler;

    public GetKeetaOrderEventLogByEventIdQueryHandlerTests()
    {
        _handler = new GetKeetaOrderEventLogByEventIdQueryHandler(_eventLogRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_EventLogNotFound_ShouldReturnNotFound()
    {
        _eventLogRepository.GetByEventIdAsync("event-1", Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrderEventLog?)null);

        var result = await _handler.Handle(new GetKeetaOrderEventLogByEventIdQuery("event-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrderEventLog.NotFound");
    }

    [Fact]
    public async Task Handle_EventLogFound_ShouldReturnMappedResponse()
    {
        var log = KeetaIntegrationOrderEventLog.Create(1, 2, "event-1", "order-1", "CONFIRMED", "{}", DateTime.UtcNow).Value;
        _eventLogRepository.GetByEventIdAsync("event-1", Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(new GetKeetaOrderEventLogByEventIdQuery("event-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EventId.Should().Be("event-1");
    }
}
