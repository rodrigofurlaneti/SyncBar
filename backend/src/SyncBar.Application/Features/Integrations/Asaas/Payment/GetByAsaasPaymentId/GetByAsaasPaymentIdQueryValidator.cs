using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.GetByAsaasPaymentId
{
    public sealed class GetByAsaasPaymentIdQueryValidator : AbstractValidator<GetByAsaasPaymentIdQuery>
    {
        public GetByAsaasPaymentIdQueryValidator()
        {
            RuleFor(x => x.AsaasPaymentId)
                .NotEmpty()
                .WithMessage("O AsaasPaymentId é obrigatório.")
                .MaximumLength(50)
                .WithMessage("O AsaasPaymentId deve ter no máximo 50 caracteres.");
        }
    }
}
