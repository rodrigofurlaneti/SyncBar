using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentIdForUpdate
{
    public sealed class GetByAsaasPaymentIdForUpdateQueryValidator : AbstractValidator<GetByAsaasPaymentIdForUpdateQuery>
    {
        public GetByAsaasPaymentIdForUpdateQueryValidator()
        {
            RuleFor(x => x.AsaasPaymentId)
                .NotEmpty()
                .WithMessage("O AsaasPaymentId é obrigatório.")
                .MaximumLength(50)
                .WithMessage("O AsaasPaymentId deve ter no máximo 50 caracteres.");
        }
    }
}
