using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

public sealed class DeleteIFoodOptionGroupCommandValidator : AbstractValidator<DeleteIFoodOptionGroupCommand>
{
    public DeleteIFoodOptionGroupCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionGroupId).NotEmpty();
    }
}
