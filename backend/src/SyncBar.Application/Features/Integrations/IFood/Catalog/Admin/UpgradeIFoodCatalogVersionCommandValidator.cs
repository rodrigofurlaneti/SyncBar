using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

public sealed class UpgradeIFoodCatalogVersionCommandValidator : AbstractValidator<UpgradeIFoodCatalogVersionCommand>
{
    public UpgradeIFoodCatalogVersionCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
    }
}
