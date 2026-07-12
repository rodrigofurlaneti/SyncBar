using FluentValidation;

namespace SyncBar.Application.Features.Comandas.Settings;

public sealed class SetComandaDefaultLimitCommandValidator : AbstractValidator<SetComandaDefaultLimitCommand>
{
    public SetComandaDefaultLimitCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.DefaultLimitAmount).GreaterThan(0);
    }
}
