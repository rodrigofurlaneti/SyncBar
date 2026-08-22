using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

public sealed class SetIFoodOptionPriceCommandValidator : AbstractValidator<SetIFoodOptionPriceCommand>
{
    public SetIFoodOptionPriceCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionId).NotEmpty();
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
    }
}
