using FluentValidation;

namespace SyncBar.Application.Features.Shift.CloseShift;

public sealed class CloseShiftClosingCommandValidator : AbstractValidator<CloseShiftClosingCommand>
{
    public CloseShiftClosingCommandValidator()
    {
        RuleFor(x => x.ShiftClosingId).GreaterThan(0);
        RuleFor(x => x.ClosedByEmployeeId).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
