using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Create
{
    public sealed class CreateKeetaIntegrationOrderEventLogCommandValidator
        : AbstractValidator<CreateKeetaIntegrationOrderEventLogCommand>
    {
        public CreateKeetaIntegrationOrderEventLogCommandValidator()
        {
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
            RuleFor(x => x.BranchId).GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
            RuleFor(x => x.EventId).NotEmpty()
                .WithMessage("O EventId é obrigatório.");
            RuleFor(x => x.OrderId).NotEmpty()
                .WithMessage("O OrderId é obrigatório.");
            RuleFor(x => x.EventType).NotEmpty()
                .WithMessage("O EventType é obrigatório.");
        }
    }
}
