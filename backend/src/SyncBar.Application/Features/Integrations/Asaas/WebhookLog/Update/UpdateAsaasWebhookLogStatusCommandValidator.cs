using FluentValidation;
using SyncBar.Domain.Enums;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Update
{
    public sealed class UpdateAsaasWebhookLogStatusCommandValidator
        : AbstractValidator<UpdateAsaasWebhookLogStatusCommand>
    {
        public UpdateAsaasWebhookLogStatusCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador do log (Id) deve ser maior que zero.");

            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Status de webhook inválido.")
                .Must(s => s == WebhookLogStatus.Processed || s == WebhookLogStatus.Failed)
                .WithMessage("O status de atualização deve ser 'Processed' ou 'Failed'.");

            When(x => x.Status == WebhookLogStatus.Failed, () =>
            {
                RuleFor(x => x.ErrorMessage)
                    .NotEmpty()
                    .WithMessage("A mensagem de erro (ErrorMessage) é obrigatória quando o status for 'Failed'.")
                    .MaximumLength(1000)
                    .WithMessage("A mensagem de erro não pode exceder 1000 caracteres.");
            });
        }
    }
}
