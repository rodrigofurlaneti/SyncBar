using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Delete
{
    public sealed class DeleteAsaasWebhookLogCommandValidator
        : AbstractValidator<DeleteAsaasWebhookLogCommand>
    {
        public DeleteAsaasWebhookLogCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador do log de webhook (Id) deve ser maior que zero.");

            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
