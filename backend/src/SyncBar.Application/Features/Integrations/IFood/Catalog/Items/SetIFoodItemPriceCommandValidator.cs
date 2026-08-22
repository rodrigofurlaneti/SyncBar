using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

public sealed class SetIFoodItemPriceCommandValidator : AbstractValidator<SetIFoodItemPriceCommand>
{
    public SetIFoodItemPriceCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
    }
}
