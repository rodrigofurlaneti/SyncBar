using FluentValidation;

namespace SyncBar.Application.Features.PublicOrdering.AddItem;

public sealed class AddPublicOrderItemCommandValidator : AbstractValidator<AddPublicOrderItemCommand>
{
    public AddPublicOrderItemCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(300);
        RuleFor(x => x.ComandaCode).MaximumLength(50);
    }
}
