using FluentValidation;

namespace SyncBar.Application.Features.Orders.Open;

public sealed class OpenOrderCommandValidator : AbstractValidator<OpenOrderCommand>
{
    public OpenOrderCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.DiningTableId.HasValue || x.ComandaId.HasValue)
            .WithMessage("Order must have a dining table or a comanda.");
        RuleFor(x => x.GuestCount).GreaterThan(0).When(x => x.GuestCount.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
