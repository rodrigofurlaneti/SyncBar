using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Merchant;

public sealed class AcknowledgeIfoodOperationalAlertCommandHandlerTests
{
    private readonly IIfoodOperationalAlertStore _alertStore = Substitute.For<IIfoodOperationalAlertStore>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AcknowledgeIfoodOperationalAlertCommandHandler _handler;

    public AcknowledgeIfoodOperationalAlertCommandHandlerTests()
    {
        _handler = new AcknowledgeIfoodOperationalAlertCommandHandler(_alertStore, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldAcknowledgeInStore()
    {
        var alertId = Guid.NewGuid();
        var command = new AcknowledgeIfoodOperationalAlertCommand(1, alertId);
        _alertStore.Acknowledge(1, alertId).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _alertStore.Received(1).Acknowledge(1, alertId);
    }

    [Fact]
    public async Task Handle_AlertAlreadyGone_ShouldStillSucceedIdempotently()
    {
        var alertId = Guid.NewGuid();
        var command = new AcknowledgeIfoodOperationalAlertCommand(1, alertId);
        _alertStore.Acknowledge(1, alertId).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
