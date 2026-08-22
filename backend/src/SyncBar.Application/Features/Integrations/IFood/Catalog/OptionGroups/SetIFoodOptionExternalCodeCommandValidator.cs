using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

public sealed class SetIFoodOptionExternalCodeCommandValidator : AbstractValidator<SetIFoodOptionExternalCodeCommand>
{
    public SetIFoodOptionExternalCodeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionId).NotEmpty();
        RuleFor(x => x.ExternalCode).NotEmpty();
    }
}
