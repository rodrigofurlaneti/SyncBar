using FluentValidation;

namespace SyncBar.Application.Features.Employees.Dismiss;

public sealed class DismissEmployeeCommandValidator : AbstractValidator<DismissEmployeeCommand>
{
    public DismissEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
    }
}
