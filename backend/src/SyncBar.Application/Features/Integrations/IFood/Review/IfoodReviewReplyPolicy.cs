using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Features.Integrations.Ifood.Review;

internal static class IfoodReviewReplyPolicy
{
    public static Result Validate(IfoodReviewDetailDto review, DateTime utcNow)
    {
        if (review.Discarded || review.Published || review.Status is "PUBLISHED" or "CREATED")
            return Result.Failure(new Error("IfoodReview.NotReplyable", "Esta avaliação não permite resposta neste estado."));

        if (review.Status == "REPLIED" || !string.IsNullOrWhiteSpace(review.Reply))
        {
            if (review.FirstReplyAt is null || utcNow >= review.FirstReplyAt.Value.ToUniversalTime().AddMinutes(10))
                return Result.Failure(new Error("IfoodReview.EditExpired", "A edição só é permitida nos 10 minutos após a primeira resposta."));
        }
        else if (review.Status != "NOT_REPLIED" || review.CreatedAt is null || utcNow >= review.CreatedAt.Value.ToUniversalTime().AddDays(5))
            return Result.Failure(new Error("IfoodReview.ReplyExpired", "O prazo de 5 dias para responder terminou ou não pôde ser confirmado."));

        return Result.Success();
    }
}
