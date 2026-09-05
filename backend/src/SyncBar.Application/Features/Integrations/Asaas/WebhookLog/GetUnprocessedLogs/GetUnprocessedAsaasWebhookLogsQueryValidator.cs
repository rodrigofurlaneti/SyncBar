using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetUnprocessedLogs
{
    public sealed class GetUnprocessedAsaasWebhookLogsQueryValidator
        : AbstractValidator<GetUnprocessedAsaasWebhookLogsQuery>
    {
        public GetUnprocessedAsaasWebhookLogsQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.Limit)
                .InclusiveBetween(1, 500)
                .WithMessage("O limite de registros deve estar entre 1 e 500.");
        }
    }
}
