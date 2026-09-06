using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Merchant;

public sealed class GetIfoodOperationalAlertsQueryHandlerTests
{
    private readonly IIfoodOperationalAlertStore _alertStore = Substitute.For<IIfoodOperationalAlertStore>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodOperationalAlertsQueryHandler _handler;

    public GetIfoodOperationalAlertsQueryHandlerTests()
    {
        _handler = new GetIfoodOperationalAlertsQueryHandler(_alertStore, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoAlerts_ShouldReturnEmptyList()
    {
        _alertStore.GetUnacknowledged(1).Returns([]);

        var result = await _handler.Handle(new GetIfoodOperationalAlertsQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AlertsExist_ShouldReturnMappedList()
    {
        var alert = new IfoodOperationalAlert(
            Guid.NewGuid(), 1, 2, "Loja Centro", "Loja indisponível", "A loja ficou offline", IfoodOperationalAlertSeverity.Critical, DateTime.UtcNow);
        _alertStore.GetUnacknowledged(1).Returns([alert]);

        var result = await _handler.Handle(new GetIfoodOperationalAlertsQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Id == alert.Id && a.Severity == "Critical" && a.BranchName == "Loja Centro");
    }
}
