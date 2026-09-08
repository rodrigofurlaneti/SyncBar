using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Integrations.Ifood.Review;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Review;

public sealed class IfoodReviewReplyPolicyTests
{
    [Theory]
    [InlineData("NOT_REPLIED", 4, null, true)]
    [InlineData("NOT_REPLIED", 5, null, false)]
    [InlineData("REPLIED", 1, 9, true)]
    [InlineData("REPLIED", 1, 10, false)]
    [InlineData("REPLIED", 1, null, false)]
    [InlineData("PUBLISHED", 1, null, false)]
    public void Validate_EnforcesRemoteStateAndDeadlines(string status, int ageDays, int? replyAgeMinutes, bool allowed)
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var review = new IfoodReviewDetailDto("id", now.AddDays(-ageDays), false, false, null, null, false, null,
            status == "REPLIED" ? "Obrigado pelo feedback" : null, 5, null, null, [], status, "PUBLIC",
            replyAgeMinutes.HasValue ? now.AddMinutes(-replyAgeMinutes.Value) : null);
        IfoodReviewReplyPolicy.Validate(review, now).IsSuccess.Should().Be(allowed);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(300, true)]
    [InlineData(301, false)]
    public void Validator_EnforcesLength(int length, bool valid)
        => new ReplyIfoodReviewCommandValidator().Validate(new ReplyIfoodReviewCommand(1, "id", new string('x', length)))
            .IsValid.Should().Be(valid);
}
