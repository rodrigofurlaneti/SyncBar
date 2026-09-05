using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetById
{
    public sealed class GetAsaasWebhookLogByIdQueryValidator
        : AbstractValidator<GetAsaasWebhookLogByIdQuery>
    {
        public GetAsaasWebhookLogByIdQueryValidator()
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
