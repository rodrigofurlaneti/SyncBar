using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByIdForUpdate
{
    public sealed class GetAsaasWebhookLogByIdForUpdateQueryValidator
        : AbstractValidator<GetAsaasWebhookLogByIdForUpdateQuery>
    {
        public GetAsaasWebhookLogByIdForUpdateQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador do log (Id) deve ser maior que zero.");

            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
