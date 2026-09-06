using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Create;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.Create;

public sealed class CreateKeetaIntegrationOrderEventLogCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository = Substitute.For<IKeetaIntegrationOrderEventLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateKeetaIntegrationOrderEventLogCommandHandler _handler;

    public CreateKeetaIntegrationOrderEventLogCommandHandlerTests()
    {
        _handler = new CreateKeetaIntegrationOrderEventLogCommandHandler(_eventLogRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_EventIdAlreadyExists_ShouldReturnConflict()
    {
        var command = new CreateKeetaIntegrationOrderEventLogCommand(1, 2, "event-1", "order-1", "CONFIRMED", "{}", DateTime.UtcNow);
        _eventLogRepository.ExistsByEventIdAsync("event-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrderEventLog.AlreadyExists");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAndReturnMappedResponse()
    {
        var command = new CreateKeetaIntegrationOrderEventLogCommand(1, 2, "event-1", "order-1", "CONFIRMED", "{}", DateTime.UtcNow);
        _eventLogRepository.ExistsByEventIdAsync("event-1", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EventId.Should().Be("event-1");
        result.Value.EventType.Should().Be("CONFIRMED");
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
