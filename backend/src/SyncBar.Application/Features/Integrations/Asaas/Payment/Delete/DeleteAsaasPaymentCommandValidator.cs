using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Payment.Delete
{
    public sealed class DeleteAsaasPaymentCommandValidator : AbstractValidator<DeleteAsaasPaymentCommand>
    {
        public DeleteAsaasPaymentCommandValidator()
        {
            RuleFor(x => x.PaymentId)
                .GreaterThan(0)
                .WithMessage("O identificador do pagamento (PaymentId) deve ser maior que zero.");
        }
    }
}
