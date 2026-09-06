using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Polling
{
    public sealed class PollKeetaEventsCommandValidator : AbstractValidator<PollKeetaEventsCommand>
    {
        public PollKeetaEventsCommandValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.BranchId)
                .GreaterThanOrEqualTo(0)
                .WithMessage("O identificador da filial (BranchId) não pode ser negativo.");
        }
    }
}
