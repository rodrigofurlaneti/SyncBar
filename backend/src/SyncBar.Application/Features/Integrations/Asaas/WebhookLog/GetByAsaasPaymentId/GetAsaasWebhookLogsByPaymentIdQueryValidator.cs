using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId
{
    public sealed class GetAsaasWebhookLogsByPaymentIdQueryValidator
        : AbstractValidator<GetAsaasWebhookLogsByPaymentIdQuery>
    {
        public GetAsaasWebhookLogsByPaymentIdQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.PaymentId)
                .NotEmpty()
                .WithMessage("O identificador do pagamento (PaymentId) é obrigatório.")
                .MaximumLength(100)
                .WithMessage("O PaymentId deve ter no máximo 100 caracteres.");
        }
    }
}
