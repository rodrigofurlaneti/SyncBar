using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.V1Legacy;

public sealed class InvokeIfoodCatalogV1OperationCommandValidator : AbstractValidator<InvokeIfoodCatalogV1OperationCommand>
{
    public InvokeIfoodCatalogV1OperationCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Operation).IsInEnum();
    }
}
