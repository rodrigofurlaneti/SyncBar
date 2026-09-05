using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetPendingByBranchId
{
    public sealed class GetPendingAsaasPaymentsByBranchIdQueryValidator
        : AbstractValidator<GetPendingAsaasPaymentsByBranchIdQuery>
    {
        public GetPendingAsaasPaymentsByBranchIdQueryValidator()
        {
            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
        }
    }
}
