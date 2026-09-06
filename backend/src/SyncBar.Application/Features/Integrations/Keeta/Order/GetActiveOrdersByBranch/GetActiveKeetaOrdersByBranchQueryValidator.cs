using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetActiveOrdersByBranch
{
    public sealed class GetActiveKeetaOrdersByBranchQueryValidator
        : AbstractValidator<GetActiveKeetaOrdersByBranchQuery>
    {
        public GetActiveKeetaOrdersByBranchQueryValidator()
        {
            RuleFor(x => x.BranchId).GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
        }
    }
}
