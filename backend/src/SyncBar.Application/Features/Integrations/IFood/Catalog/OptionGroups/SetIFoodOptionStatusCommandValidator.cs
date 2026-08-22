using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

public sealed class SetIFoodOptionStatusCommandValidator : AbstractValidator<SetIFoodOptionStatusCommand>
{
    public SetIFoodOptionStatusCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionId).NotEmpty();
    }
}
