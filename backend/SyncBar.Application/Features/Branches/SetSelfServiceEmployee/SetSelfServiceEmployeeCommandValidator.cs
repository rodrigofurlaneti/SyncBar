using FluentValidation;

namespace SyncBar.Application.Features.Branches.SetSelfServiceEmployee;

public sealed class SetSelfServiceEmployeeCommandValidator : AbstractValidator<SetSelfServiceEmployeeCommand>
{
    public SetSelfServiceEmployeeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
    }
}
