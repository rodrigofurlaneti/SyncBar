using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

public sealed class DowngradeIFoodCatalogVersionCommandValidator : AbstractValidator<DowngradeIFoodCatalogVersionCommand>
{
    public DowngradeIFoodCatalogVersionCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
    }
}
