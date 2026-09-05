using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Create
{
    public sealed class CreateAsaasWebhookLogCommandValidator
        : AbstractValidator<CreateAsaasWebhookLogCommand>
    {
        public CreateAsaasWebhookLogCommandValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            When(x => x.BranchId.HasValue, () =>
            {
                RuleFor(x => x.BranchId!.Value)
                    .GreaterThan(0)
                    .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
            });

            RuleFor(x => x.Event)
                .NotEmpty()
                .WithMessage("O tipo do evento (Event) é obrigatório.")
                .MaximumLength(100)
                .WithMessage("O tipo de evento deve ter no máximo 100 caracteres.");

            When(x => !string.IsNullOrWhiteSpace(x.AsaasEventId), () =>
            {
                RuleFor(x => x.AsaasEventId)
                    .MaximumLength(150)
                    .WithMessage("O AsaasEventId não pode exceder 150 caracteres.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.PaymentId), () =>
            {
                RuleFor(x => x.PaymentId)
                    .MaximumLength(100)
                    .WithMessage("O identificador do pagamento (PaymentId) não pode exceder 100 caracteres.");
            });

            RuleFor(x => x.Payload)
                .NotEmpty()
                .WithMessage("O payload JSON do webhook é obrigatório.");

            When(x => !string.IsNullOrWhiteSpace(x.IpAddress), () =>
            {
                RuleFor(x => x.IpAddress)
                    .MaximumLength(45)
                    .WithMessage("O endereço de IP não pode exceder 45 caracteres.");
            });
        }
    }
}
