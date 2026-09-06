using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Infrastructure.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class InMemoryIfoodOperationalAlertStoreTests
{
    private readonly InMemoryIfoodOperationalAlertStore _store = new();

    [Fact]
    public void Raise_ShouldReturnAlertWithProvidedDataAndGeneratedIdAndTimestamp()
    {
        var before = DateTime.Now;

        var alert = _store.Raise(1, 2, "Loja Centro", "Loja indisponível", "A loja ficou indisponível no Ifood.", IfoodOperationalAlertSeverity.Critical);

        alert.Id.Should().NotBeEmpty();
        alert.CompanyId.Should().Be(1);
        alert.BranchId.Should().Be(2);
        alert.BranchName.Should().Be("Loja Centro");
        alert.Title.Should().Be("Loja indisponível");
        alert.Message.Should().Be("A loja ficou indisponível no Ifood.");
        alert.Severity.Should().Be(IfoodOperationalAlertSeverity.Critical);
        alert.CreatedAtUtc.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void GetUnacknowledged_NoAlertsForCompany_ShouldReturnEmptyList()
    {
        var result = _store.GetUnacknowledged(999);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetUnacknowledged_MultipleCompanies_ShouldOnlyReturnAlertsForRequestedCompany()
    {
        _store.Raise(1, 1, "Loja A", "t1", "m1", IfoodOperationalAlertSeverity.Info);
        _store.Raise(2, 1, "Loja B", "t2", "m2", IfoodOperationalAlertSeverity.Info);

        var result = _store.GetUnacknowledged(1);

        result.Should().ContainSingle(a => a.CompanyId == 1);
    }

    [Fact]
    public void GetUnacknowledged_ShouldReturnMostRecentFirst()
    {
        var first = _store.Raise(1, 1, "Loja A", "t1", "m1", IfoodOperationalAlertSeverity.Info);
        Thread.Sleep(5);
        var second = _store.Raise(1, 1, "Loja A", "t2", "m2", IfoodOperationalAlertSeverity.Info);

        var result = _store.GetUnacknowledged(1);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(second.Id);
        result[1].Id.Should().Be(first.Id);
    }

    [Fact]
    public void Acknowledge_ExistingAlert_ShouldRemoveItAndReturnTrue()
    {
        var alert = _store.Raise(1, 1, "Loja A", "t1", "m1", IfoodOperationalAlertSeverity.Warning);

        var acknowledged = _store.Acknowledge(1, alert.Id);

        acknowledged.Should().BeTrue();
        _store.GetUnacknowledged(1).Should().BeEmpty();
    }

    [Fact]
    public void Acknowledge_UnknownCompany_ShouldReturnFalse()
    {
        var acknowledged = _store.Acknowledge(999, Guid.NewGuid());

        acknowledged.Should().BeFalse();
    }

    [Fact]
    public void Acknowledge_UnknownAlertIdForExistingCompany_ShouldReturnFalse()
    {
        _store.Raise(1, 1, "Loja A", "t1", "m1", IfoodOperationalAlertSeverity.Info);

        var acknowledged = _store.Acknowledge(1, Guid.NewGuid());

        acknowledged.Should().BeFalse();
        _store.GetUnacknowledged(1).Should().HaveCount(1);
    }

    [Fact]
    public void Raise_MoreThanMaxPerCompany_ShouldEvictOldestAlerts()
    {
        for (var i = 0; i < 51; i++)
            _store.Raise(1, 1, "Loja A", $"t{i}", $"m{i}", IfoodOperationalAlertSeverity.Info);

        var result = _store.GetUnacknowledged(1);

        result.Should().HaveCount(50);
        result.Should().NotContain(a => a.Title == "t0");
        result.Should().Contain(a => a.Title == "t50");
    }
}
