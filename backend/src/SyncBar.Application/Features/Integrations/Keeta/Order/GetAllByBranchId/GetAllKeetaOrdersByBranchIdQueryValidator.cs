using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetAllByBranchId
{
    public sealed class GetAllKeetaOrdersByBranchIdQueryValidator
        : AbstractValidator<GetAllKeetaOrdersByBranchIdQuery>
    {
        public GetAllKeetaOrdersByBranchIdQueryValidator()
        {
            RuleFor(x => x.BranchId).GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
        }
    }
}
