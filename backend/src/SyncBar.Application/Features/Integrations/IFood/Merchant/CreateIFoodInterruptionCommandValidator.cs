using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

// Duração de 1 minuto a 7 dias — limite confirmado na doc oficial do módulo Merchant (Fase 5).
public sealed class CreateIfoodInterruptionCommandValidator : AbstractValidator<CreateIfoodInterruptionCommand>
{
    public CreateIfoodInterruptionCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(255);
        RuleFor(x => x.End).GreaterThan(x => x.Start).WithMessage("End must be after start.");
        RuleFor(x => x).Must(x => (x.End - x.Start) >= TimeSpan.FromMinutes(1) && (x.End - x.Start) <= TimeSpan.FromDays(7))
            .WithMessage("Interruption duration must be between 1 minute and 7 days.");
    }
}
