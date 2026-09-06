using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Delete;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.Delete;

public sealed class DeleteKeetaIntegrationOrderEventLogCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository = Substitute.For<IKeetaIntegrationOrderEventLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeleteKeetaIntegrationOrderEventLogCommandHandler _handler;

    public DeleteKeetaIntegrationOrderEventLogCommandHandlerTests()
    {
        _handler = new DeleteKeetaIntegrationOrderEventLogCommandHandler(_eventLogRepository, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationOrderEventLog MakeLog(long companyId = 1) =>
        KeetaIntegrationOrderEventLog.Create(companyId, 2, "event-1", "order-1", "CONFIRMED", "{}", DateTime.UtcNow).Value;

    [Fact]
    public async Task Handle_EventLogNotFound_ShouldReturnNotFound()
    {
        var command = new DeleteKeetaIntegrationOrderEventLogCommand(1, 1);
        _eventLogRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrderEventLog?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrderEventLog.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var log = MakeLog(1);
        var command = new DeleteKeetaIntegrationOrderEventLogCommand(1, 2);
        _eventLogRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrderEventLog.NotFound");
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDeleteAndCommit()
    {
        var log = MakeLog(1);
        var command = new DeleteKeetaIntegrationOrderEventLogCommand(1, 1);
        _eventLogRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _eventLogRepository.Received(1).Delete(log);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
