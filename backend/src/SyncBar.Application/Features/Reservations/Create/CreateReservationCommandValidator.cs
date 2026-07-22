using FluentValidation;

namespace SyncBar.Application.Features.Reservations.Create;

public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CustomerPhone).MaximumLength(20);
        RuleFor(x => x.PartySize).GreaterThan(0);
        RuleFor(x => x.ReservedFor).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
