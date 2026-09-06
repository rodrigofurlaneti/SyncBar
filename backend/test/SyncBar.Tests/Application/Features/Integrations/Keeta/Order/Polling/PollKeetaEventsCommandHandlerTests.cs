using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Order.Polling;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.Order.Polling;

public sealed class PollKeetaEventsCommandHandlerTests
{
    private readonly IKeetaOrderClient _orderClient = Substitute.For<IKeetaOrderClient>();
    private readonly IKeetaOrderEventProcessor _eventProcessor = Substitute.For<IKeetaOrderEventProcessor>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly PollKeetaEventsCommandHandler _handler;

    public PollKeetaEventsCommandHandlerTests()
    {
        _handler = new PollKeetaEventsCommandHandler(_orderClient, _eventProcessor, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoEventsPolled_ShouldReturnZeroedResponseWithoutAcknowledging()
    {
        var command = new PollKeetaEventsCommand(1, 2);
        _orderClient.PollEventsAsync(1, 2, null, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(0);
        await _orderClient.DidNotReceive().AcknowledgeEventsAsync(
            Arg.Any<long>(), Arg.Any<long>(), Arg.Any<IReadOnlyList<KeetaPolledEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventsPolled_ShouldProcessEachAndAcknowledgeAll()
    {
        var command = new PollKeetaEventsCommand(1, 2, ["im-1"]);
        var events = new List<KeetaPolledEvent>
        {
            new("evt-1", "CONFIRMED", "order-1", "https://x", DateTime.UtcNow, "{}"),
            new("evt-2", "CREATED", "order-2", "https://y", DateTime.UtcNow, "{}"),
        };
        _orderClient.PollEventsAsync(1, 2, Arg.Is<IReadOnlyList<string>>(l => l.Contains("im-1")), Arg.Any<CancellationToken>()).Returns(events);
        _eventProcessor.ProcessAsync(1, 2, events[0], Arg.Any<CancellationToken>()).Returns(true);
        _eventProcessor.ProcessAsync(1, 2, events[1], Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(2);
        result.Value.ProcessedEvents.Should().Be(1);
        result.Value.UnprocessedEvents.Should().Be(1);
        await _orderClient.Received(1).AcknowledgeEventsAsync(1, 2, events, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
