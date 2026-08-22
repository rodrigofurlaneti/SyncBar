using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

public sealed class BatchUpdateIFoodProductStatusesCommandValidator : AbstractValidator<BatchUpdateIFoodProductStatusesCommand>
{
    public BatchUpdateIFoodProductStatusesCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
    }
}
