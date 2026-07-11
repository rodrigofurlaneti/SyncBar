using FluentValidation;

namespace SyncBar.Application.Features.Printing.SetSettings;

public sealed class SetPrintSettingsCommandValidator : AbstractValidator<SetPrintSettingsCommand>
{
    public SetPrintSettingsCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
    }
}
