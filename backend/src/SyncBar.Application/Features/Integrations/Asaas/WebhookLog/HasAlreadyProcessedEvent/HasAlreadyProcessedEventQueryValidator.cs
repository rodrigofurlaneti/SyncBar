using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.HasAlreadyProcessedEvent
{
    public sealed class HasAlreadyProcessedEventQueryValidator
        : AbstractValidator<HasAlreadyProcessedEventQuery>
    {
        public HasAlreadyProcessedEventQueryValidator()
        {
            RuleFor(x => x.AsaasEventId)
                .NotEmpty()
                .WithMessage("O identificador do evento Asaas (AsaasEventId) é obrigatório.")
                .MaximumLength(150)
                .WithMessage("O AsaasEventId deve ter no máximo 150 caracteres.");
        }
    }
}
