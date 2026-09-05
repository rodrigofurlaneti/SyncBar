using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.Payment.ExistsByAsaasPaymentId
{
    public sealed class ExistsByAsaasPaymentIdQueryValidator : AbstractValidator<ExistsByAsaasPaymentIdQuery>
    {
        public ExistsByAsaasPaymentIdQueryValidator()
        {
            RuleFor(x => x.AsaasPaymentId)
                .NotEmpty()
                .WithMessage("O AsaasPaymentId é obrigatório.")
                .MaximumLength(50)
                .WithMessage("O AsaasPaymentId deve ter no máximo 50 caracteres.");
        }
    }
}
