using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

public sealed class BatchUpdateIfoodProductStatusesCommandValidator : AbstractValidator<BatchUpdateIfoodProductStatusesCommand>
{
    public BatchUpdateIfoodProductStatusesCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
    }
}
