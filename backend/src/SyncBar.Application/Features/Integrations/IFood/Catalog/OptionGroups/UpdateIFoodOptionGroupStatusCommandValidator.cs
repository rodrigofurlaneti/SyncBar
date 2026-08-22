using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

public sealed class UpdateIFoodOptionGroupStatusCommandValidator : AbstractValidator<UpdateIFoodOptionGroupStatusCommand>
{
    public UpdateIFoodOptionGroupStatusCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionGroupId).NotEmpty();
    }
}
