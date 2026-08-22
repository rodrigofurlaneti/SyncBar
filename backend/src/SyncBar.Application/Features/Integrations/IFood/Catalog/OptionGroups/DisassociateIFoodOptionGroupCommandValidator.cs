using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

public sealed class DisassociateIFoodOptionGroupCommandValidator : AbstractValidator<DisassociateIFoodOptionGroupCommand>
{
    public DisassociateIFoodOptionGroupCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.OptionGroupId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
