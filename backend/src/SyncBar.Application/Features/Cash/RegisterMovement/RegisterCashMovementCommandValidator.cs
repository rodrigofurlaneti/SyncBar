using FluentValidation;
using SyncBar.Domain.Constants;

namespace SyncBar.Application.Features.Cash.RegisterMovement;

public sealed class RegisterCashMovementCommandValidator : AbstractValidator<RegisterCashMovementCommand>
{
    public RegisterCashMovementCommandValidator()
    {
        RuleFor(x => x.CashSessionId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(300);
        RuleFor(x => x.CashMovementTypeId).Must(t =>
                t is CashMovementTypeIds.Suprimento or CashMovementTypeIds.Sangria or CashMovementTypeIds.Despesa)
            .WithMessage("Movement type must be Suprimento, Sangria or Despesa.");
    }
}
