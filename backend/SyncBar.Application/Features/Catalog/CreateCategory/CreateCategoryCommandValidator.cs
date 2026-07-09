using FluentValidation;

namespace SyncBar.Application.Features.Catalog.CreateCategory;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
