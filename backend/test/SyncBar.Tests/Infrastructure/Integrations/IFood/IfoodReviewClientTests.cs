using System.Net;
using FluentAssertions;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodReviewClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly IfoodReviewClient _client;

    public IfoodReviewClientTests()
    {
        _client = new IfoodReviewClient(new HttpClient(_handler));
    }

    [Fact]
    public async Task GetReviewsAsync_Success_ShouldMapListAndPaginationFields()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"page":1,"size":10,"total":2,"pageCount":1,"reviews":[
                {"id":"rev-1","createdAt":"2026-01-01T00:00:00Z","discarded":false,"published":true,"comment":"Muito bom","moderated":false,"score":5,
                 "order":{"id":"order-1","shortId":"#1","createdAt":"2026-01-01T00:00:00Z"}},
                {"id":"rev-2","createdAt":"2026-01-02T00:00:00Z","discarded":true,"published":false,"score":2}
            ]}
            """);

        var result = await _client.GetReviewsAsync("tok", "MERCH-1", 1, 10, true, null, null, "asc", "createdAt", CancellationToken.None);

        result.Page.Should().Be(1);
        result.Total.Should().Be(2);
        result.Reviews.Should().HaveCount(2);
        var first = result.Reviews.First(r => r.Id == "rev-1");
        first.Comment.Should().Be("Muito bom");
        first.Order!.Id.Should().Be("order-1");
        var request = _handler.Requests[^1];
        request.Headers.Authorization!.Parameter.Should().Be("tok");
        request.RequestUri!.ToString().Should().Contain("page=1").And.Contain("addCount=true").And.Contain("sort=asc").And.Contain("sortBy=createdAt");
    }

    [Fact]
    public async Task GetReviewsAsync_WithDateRange_ShouldIncludeDateFromAndDateToInQuery()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"page":1,"size":10,"total":0,"pageCount":0,"reviews":[]}""");
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        await _client.GetReviewsAsync("tok", "MERCH-1", 1, 10, false, from, to, "desc", "score", CancellationToken.None);

        var url = _handler.Requests[^1].RequestUri!.ToString();
        url.Should().Contain("dateFrom=").And.Contain("dateTo=");
    }

    [Fact]
    public async Task GetReviewsAsync_HttpFailure_ShouldReturnEmptyResultWithRequestedPageAndSize()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetReviewsAsync("tok", "MERCH-1", 2, 25, false, null, null, "asc", "score", CancellationToken.None);

        result.Page.Should().Be(2);
        result.Size.Should().Be(25);
        result.Total.Should().Be(0);
        result.Reviews.Should().BeEmpty();
    }

    [Fact]
    public async Task GetReviewsAsync_MissingReviewsArray_ShouldReturnEmptyList()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"page":1,"size":10}""");

        var result = await _client.GetReviewsAsync("tok", "MERCH-1", 1, 10, false, null, null, "asc", "score", CancellationToken.None);

        result.Reviews.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetReviewByIdAsync_Success_ShouldMapDetailWithQuestionsAndAnswers()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"id":"rev-1","createdAt":"2026-01-01T00:00:00Z","discarded":false,"published":true,"comment":"Otimo",
             "customerName":"Cliente X","moderated":true,"moderationStatus":"APPROVED","reply":"Obrigado","score":4.5,
             "surveyId":"survey-1","order":{"id":"order-1","shortId":"#1"},
             "questions":[{"id":"q-1","type":"single-choice","title":"Como foi?","answers":[{"id":"a-1","title":"Bom"}]}]}
            """);

        var result = await _client.GetReviewByIdAsync("tok", "MERCH-1", "rev-1", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("rev-1");
        result.CustomerName.Should().Be("Cliente X");
        result.Questions.Should().ContainSingle(q => q.Id == "q-1" && q.Answers.Count == 1 && q.Answers.First().Title == "Bom");
        result.Order!.ShortId.Should().Be("#1");
    }

    [Fact]
    public async Task GetReviewByIdAsync_Failure_ShouldReturnNull()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "");

        var result = await _client.GetReviewByIdAsync("tok", "MERCH-1", "rev-missing", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReviewByIdAsync_NoQuestionsOrOrder_ShouldReturnEmptyQuestionsAndNullOrder()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"rev-1","discarded":false,"published":true}""");

        var result = await _client.GetReviewByIdAsync("tok", "MERCH-1", "rev-1", CancellationToken.None);

        result!.Questions.Should().BeEmpty();
        result.Order.Should().BeNull();
    }

    [Fact]
    public async Task ReplyReviewAsync_Success_ShouldPostTextAndMapResponse()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"createdAt":"2026-01-01T00:00:00Z","text":"Obrigado pela avaliacao","reviewId":"rev-1"}""");

        var result = await _client.ReplyReviewAsync("tok", "MERCH-1", "rev-1", "Obrigado pela avaliacao", CancellationToken.None);

        result.Text.Should().Be("Obrigado pela avaliacao");
        result.ReviewId.Should().Be("rev-1");
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("reviews/rev-1/answers");
        _handler.RequestBodies[^1].Should().Contain("Obrigado pela avaliacao");
    }

    [Fact]
    public async Task ReplyReviewAsync_Failure_ShouldThrowWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "texto muito longo");

        var act = () => _client.ReplyReviewAsync("tok", "MERCH-1", "rev-1", "x", CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*400*texto muito longo*");
    }

    [Fact]
    public async Task ReplyReviewAsync_UnparsableSuccessBody_ShouldFallBackToEchoedTextAndReviewId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "not-json");

        var result = await _client.ReplyReviewAsync("tok", "MERCH-1", "rev-1", "Obrigado", CancellationToken.None);

        result.Text.Should().Be("Obrigado");
        result.ReviewId.Should().Be("rev-1");
        result.CreatedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetSummaryAsync_Success_ShouldMapScoreAndCounts()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"score":4.8,"totalReviewsCount":100,"validReviewsCount":90}""");

        var result = await _client.GetSummaryAsync("tok", "MERCH-1", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Score.Should().Be(4.8);
        result.TotalReviewsCount.Should().Be(100);
        result.ValidReviewsCount.Should().Be(90);
    }

    [Fact]
    public async Task GetSummaryAsync_Failure_ShouldReturnNull()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetSummaryAsync("tok", "MERCH-1", CancellationToken.None);

        result.Should().BeNull();
    }
}
