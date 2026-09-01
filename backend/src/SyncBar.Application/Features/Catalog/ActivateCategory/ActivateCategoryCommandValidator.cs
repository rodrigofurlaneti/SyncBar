using FluentValidation;

namespace SyncBar.Application.Features.Catalog.ActivateCategory;

public sealed class ActivateCategoryCommandValidator : AbstractValidator<ActivateCategoryCommand>
{
    public ActivateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
    }
}
