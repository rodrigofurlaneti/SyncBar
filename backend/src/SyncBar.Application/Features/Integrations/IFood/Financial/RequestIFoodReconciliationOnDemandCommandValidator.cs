using FluentValidation;

namespace SyncBar.Application.Features.Integrations.IFood.Financial;

public sealed class RequestIFoodReconciliationOnDemandCommandValidator : AbstractValidator<RequestIFoodReconciliationOnDemandCommand>
{
    public RequestIFoodReconciliationOnDemandCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Competence).NotEmpty().Matches(@"^\d{4}-\d{2}$")
            .WithMessage("Competence must be in the yyyy-MM format.");
    }
}
