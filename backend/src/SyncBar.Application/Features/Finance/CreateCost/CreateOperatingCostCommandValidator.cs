using FluentValidation;
using SyncBar.Domain.Constants;

namespace SyncBar.Application.Features.Finance.CreateCost;

public sealed class CreateOperatingCostCommandValidator : AbstractValidator<CreateOperatingCostCommand>
{
    public CreateOperatingCostCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CostTypeId)
            .Must(t => t is CostTypeIds.Fixo or CostTypeIds.Variavel)
            .WithMessage("Tipo de custo deve ser Fixo ou Variável.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ReferenceMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.ReferenceYear).InclusiveBetween(2000, 2100);
    }
}
