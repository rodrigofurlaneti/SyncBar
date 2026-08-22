using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

public sealed class SetIFoodItemExternalCodeCommandValidator : AbstractValidator<SetIFoodItemExternalCodeCommand>
{
    public SetIFoodItemExternalCodeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ItemId).NotEmpty();
    }
}
