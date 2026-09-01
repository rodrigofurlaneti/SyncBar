using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed class SaveIfoodOpeningHoursCommandValidator : AbstractValidator<SaveIfoodOpeningHoursCommand>
{
    public SaveIfoodOpeningHoursCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleForEach(x => x.Shifts).ChildRules(shift =>
        {
            shift.RuleFor(x => x.DayOfWeek).InclusiveBetween(0, 6);
            shift.RuleFor(x => x.Start).NotEmpty().Matches(@"^([01]\d|2[0-3]):[0-5]\d$").WithMessage("Start must be in HH:mm format.");
            shift.RuleFor(x => x.DurationMinutes).GreaterThan(0);
        });
    }
}
