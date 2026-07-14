using FluentValidation;

namespace SyncBar.Application.Features.Employees.SetCommission;

public sealed class SetCommissionCommandValidator : AbstractValidator<SetCommissionCommand>
{
    public SetCommissionCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.CommissionPercent).InclusiveBetween(0, 100).When(x => x.CommissionPercent.HasValue);
    }
}
