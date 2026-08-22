using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.V1Legacy;

public sealed class InvokeIFoodCatalogV1OperationCommandValidator : AbstractValidator<InvokeIFoodCatalogV1OperationCommand>
{
    public InvokeIFoodCatalogV1OperationCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Operation).IsInEnum();
    }
}
