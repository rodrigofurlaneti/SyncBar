using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Delete
{
    public sealed class DeleteKeetaIntegrationOrderEventLogCommandValidator
        : AbstractValidator<DeleteKeetaIntegrationOrderEventLogCommand>
    {
        public DeleteKeetaIntegrationOrderEventLogCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador do evento (Id) deve ser maior que zero.");
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
