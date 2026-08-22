using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

public sealed class UpdateIFoodOptionGroupCommandValidator : AbstractValidator<UpdateIFoodOptionGroupCommand>
{
    public UpdateIFoodOptionGroupCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionGroupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
