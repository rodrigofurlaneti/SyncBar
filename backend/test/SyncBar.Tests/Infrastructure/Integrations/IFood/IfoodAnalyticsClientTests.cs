using System.Net;
using FluentAssertions;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodAnalyticsClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly IfoodAnalyticsClient _client;

    public IfoodAnalyticsClientTests()
    {
        _client = new IfoodAnalyticsClient(new HttpClient(_handler));
    }

    [Fact]
    public async Task GetOrderKpisAsync_Success_ShouldMapBucketsAndCurrentPage()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"currentPage":2,"data":[{"salesChannel":"IFOOD"},{"salesChannel":"WHITE_LABEL"}]}""");

        var result = await _client.GetOrderKpisAsync("tok", "MERCH-1", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), 2, 20, CancellationToken.None);

        result.CurrentPage.Should().Be(2);
        result.RawBuckets.Should().HaveCount(2);
        var request = _handler.Requests[^1];
        request.Method.Should().Be(HttpMethod.Post);
        request.Headers.Authorization!.Parameter.Should().Be("tok");
        request.RequestUri!.ToString().Should().Contain("MERCH-1/orders/kpis");
        var body = _handler.RequestBodies[^1]!;
        body.Should().Contain("\"gte\":\"2026-01-01\"");
        body.Should().Contain("\"lte\":\"2026-01-31\"");
    }

    [Fact]
    public async Task GetOrderKpisAsync_HttpFailure_ShouldNotOverwriteSnapshotsWithEmptyData()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var action = () => _client.GetOrderKpisAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, 3, 10, CancellationToken.None);
        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetOrderKpisAsync_ResponseWithoutCurrentPage_ShouldFallBackToRequestedPage()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"data":[]}""");

        var result = await _client.GetOrderKpisAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, 5, 10, CancellationToken.None);

        result.CurrentPage.Should().Be(5);
    }

    [Fact]
    public async Task GetOrderKpisAsync_ResponseWithoutDataArray_ShouldReturnEmptyBuckets()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"currentPage":1}""");

        var result = await _client.GetOrderKpisAsync("tok", "MERCH-1", DateTime.Today, DateTime.Today, 1, 10, CancellationToken.None);

        result.RawBuckets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderKpisAsync_MerchantIdWithSlash_ShouldBeUrlEscaped()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"currentPage":1,"data":[]}""");

        await _client.GetOrderKpisAsync("tok", "MERCH/1", DateTime.Today, DateTime.Today, 1, 10, CancellationToken.None);

        _handler.Requests[^1].RequestUri!.ToString().Should().Contain("MERCH%2F1");
    }
}
