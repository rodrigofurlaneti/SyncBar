using FluentValidation;

namespace SyncBar.Application.Features.Orders.SetTableReadingValidation;

public sealed class SetTableReadingValidationCommandValidator : AbstractValidator<SetTableReadingValidationCommand>
{
    public SetTableReadingValidationCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
    }
}
