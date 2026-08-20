using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Review;

public sealed class ReplyIFoodReviewCommandValidator : AbstractValidator<ReplyIFoodReviewCommand>
{
    public ReplyIFoodReviewCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
    }
}
