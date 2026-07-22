using FluentValidation;

namespace SyncBar.Application.Features.Employees.Update;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.JobTitleId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(150).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Salary).GreaterThanOrEqualTo(0).When(x => x.Salary.HasValue);
    }
}
