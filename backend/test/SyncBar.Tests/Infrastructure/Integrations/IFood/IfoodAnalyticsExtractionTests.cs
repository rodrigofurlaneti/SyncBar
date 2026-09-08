using System.Text;
using FluentAssertions;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodAnalyticsExtractionTests
{
    [Theory]
    [InlineData("2026-09-08T11:59:00Z", false)]
    [InlineData("2026-09-08T12:04:00Z", false)]
    [InlineData("2026-09-08T12:05:00Z", true)]
    public void Schedule_UsesBrasiliaAfterNine(string utc, bool available)
        => IfoodAnalyticsExtractionBackgroundService.SnapshotAvailable(DateTimeOffset.Parse(utc)).Should().Be(available);

    [Theory]
    [InlineData("analytics merchant_scope", true)]
    [InlineData("analytics chain_scope", false)]
    [InlineData("analytics merchant_scope chain_scope", false)]
    [InlineData("merchant_scope", false)]
    public void Scopes_RejectMixedModels(string scope, bool valid)
    {
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"scope\":\"" + scope + "\"}"));
        var action = () => IfoodAnalyticsExtractionBackgroundService.ValidateMerchantScopes("header." + payload + ".signature");
        if (valid) action.Should().NotThrow(); else action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Snapshot_RecalculationReplacesRatherThanAccumulates()
    {
        var snapshot = IfoodAnalyticsSnapshot.Create(1, "merchant", new DateTime(2026, 9, 7));
        snapshot.Replace("[{\"gmv\":100}]", DateTime.UtcNow);
        snapshot.Replace("[{\"gmv\":80}]", DateTime.UtcNow);
        snapshot.AggregatesJson.Should().Be("[{\"gmv\":80}]");
    }
}
