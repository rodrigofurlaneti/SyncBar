using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetAllByOrderId;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.GetAllByOrderId;

public sealed class GetAllKeetaOrderEventLogsByOrderIdQueryHandlerTests
{
    private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository = Substitute.For<IKeetaIntegrationOrderEventLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetAllKeetaOrderEventLogsByOrderIdQueryHandler _handler;

    public GetAllKeetaOrderEventLogsByOrderIdQueryHandlerTests()
    {
        _handler = new GetAllKeetaOrderEventLogsByOrderIdQueryHandler(_eventLogRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_EventLogsExist_ShouldReturnMappedList()
    {
        var logs = new List<KeetaIntegrationOrderEventLog>
        {
            KeetaIntegrationOrderEventLog.Create(1, 2, "event-1", "order-1", "CONFIRMED", "{}", DateTime.UtcNow).Value,
        };
        _eventLogRepository.GetAllByOrderIdAsync("order-1", Arg.Any<CancellationToken>()).Returns(logs);

        var result = await _handler.Handle(new GetAllKeetaOrderEventLogsByOrderIdQuery("order-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_NoEventLogsForOrder_ShouldReturnEmptyList()
    {
        _eventLogRepository.GetAllByOrderIdAsync("order-1", Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(new GetAllKeetaOrderEventLogsByOrderIdQuery("order-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
