using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed class SaveIfoodOpeningHoursCommandValidator : AbstractValidator<SaveIfoodOpeningHoursCommand>
{
    public SaveIfoodOpeningHoursCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Shifts).Must(NoOverlap).WithMessage("Os turnos não podem se sobrepor, inclusive quando atravessam a meia-noite.");
        RuleForEach(x => x.Shifts).ChildRules(shift =>
        {
            shift.RuleFor(x => x.DayOfWeek).InclusiveBetween(0, 6);
            shift.RuleFor(x => x.Start).NotEmpty().Matches(@"^([01]\d|2[0-3]):[0-5]\d$").WithMessage("Start must be in HH:mm format.");
            shift.RuleFor(x => x.DurationMinutes).GreaterThan(0);
        });
    }

    internal static bool NoOverlap(IReadOnlyCollection<IfoodOpeningHourShiftInput> shifts)
    {
        if (shifts is null) return false;
        var intervals = shifts.Where(shift => TimeSpan.TryParse(shift.Start, out _))
            .Select(shift => (Start: shift.DayOfWeek * 1440 + TimeSpan.Parse(shift.Start).TotalMinutes, Duration: shift.DurationMinutes)).ToArray();
        for (var i = 0; i < intervals.Length; i++)
        for (var j = i + 1; j < intervals.Length; j++)
        foreach (var offset in new[] { -10080, 0, 10080 })
        {
            var a = intervals[i];
            var b = intervals[j];
            if (a.Start < b.Start + offset + b.Duration && b.Start + offset < a.Start + a.Duration) return false;
        }
        return true;
    }
}
