using FluentValidation;

namespace SyncBar.Application.Features.Tables.SetReadingValidation;

public sealed class SetDiningTableReadingValidationCommandValidator : AbstractValidator<SetDiningTableReadingValidationCommand>
{
    public SetDiningTableReadingValidationCommandValidator()
    {
        RuleFor(x => x.DiningTableId).GreaterThan(0);
    }
}
