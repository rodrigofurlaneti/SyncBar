using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Update
{
    public sealed class UpdateKeetaIntegrationOrderEventLogCommandValidator
        : AbstractValidator<UpdateKeetaIntegrationOrderEventLogCommand>
    {
        public UpdateKeetaIntegrationOrderEventLogCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador do evento (Id) deve ser maior que zero.");
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
