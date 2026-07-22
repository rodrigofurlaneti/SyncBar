using FluentValidation;

namespace SyncBar.Application.Features.Reservations.Confirm;

public sealed class ConfirmReservationCommandValidator : AbstractValidator<ConfirmReservationCommand>
{
    public ConfirmReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId).GreaterThan(0);
        RuleFor(x => x.DiningTableId).GreaterThan(0);
    }
}
