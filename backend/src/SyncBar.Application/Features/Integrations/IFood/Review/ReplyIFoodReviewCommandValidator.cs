using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Review;

public sealed class ReplyIfoodReviewCommandValidator : AbstractValidator<ReplyIfoodReviewCommand>
{
    public ReplyIfoodReviewCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
    }
}
