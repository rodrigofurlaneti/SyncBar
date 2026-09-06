using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Keeta.OrderEventLog.Update;

public sealed class UpdateKeetaIntegrationOrderEventLogCommandHandlerTests
{
    private readonly IKeetaIntegrationOrderEventLogRepository _eventLogRepository = Substitute.For<IKeetaIntegrationOrderEventLogRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateKeetaIntegrationOrderEventLogCommandHandler _handler;

    public UpdateKeetaIntegrationOrderEventLogCommandHandlerTests()
    {
        _handler = new UpdateKeetaIntegrationOrderEventLogCommandHandler(_eventLogRepository, _logRepository, _unitOfWork);
    }

    private static KeetaIntegrationOrderEventLog MakeLog(long companyId = 1) =>
        KeetaIntegrationOrderEventLog.Create(companyId, 2, "event-1", "order-1", "CONFIRMED", "{}", DateTime.UtcNow).Value;

    [Fact]
    public async Task Handle_EventLogNotFound_ShouldReturnNotFound()
    {
        var command = new UpdateKeetaIntegrationOrderEventLogCommand(1, 1, MarkAsProcessed: true);
        _eventLogRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationOrderEventLog?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrderEventLog.NotFound");
    }

    [Fact]
    public async Task Handle_CompanyMismatch_ShouldReturnNotFound()
    {
        var log = MakeLog(1);
        var command = new UpdateKeetaIntegrationOrderEventLogCommand(1, 2, MarkAsProcessed: true);
        _eventLogRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("KeetaOrderEventLog.NotFound");
    }

    [Fact]
    public async Task Handle_MarkAsProcessed_ShouldSetProcessedSuccessfullyTrue()
    {
        var log = MakeLog(1);
        var command = new UpdateKeetaIntegrationOrderEventLogCommand(1, 1, MarkAsProcessed: true);
        _eventLogRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        log.ProcessedSuccessfully.Should().BeTrue();
        _eventLogRepository.Received(1).Update(log);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ErrorMessageProvided_ShouldMarkAsFailed()
    {
        var log = MakeLog(1);
        var command = new UpdateKeetaIntegrationOrderEventLogCommand(1, 1, ErrorMessage: "falha ao processar");
        _eventLogRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(log);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        log.ProcessedSuccessfully.Should().BeFalse();
        log.ErrorMessage.Should().Be("falha ao processar");
    }
}
