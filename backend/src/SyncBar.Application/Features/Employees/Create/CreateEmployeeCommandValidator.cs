using FluentValidation;

namespace SyncBar.Application.Features.Employees.Create;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.JobTitleId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Cpf).NotEmpty().Length(11).Matches("^[0-9]+$").WithMessage("CPF deve ter 11 dígitos numéricos.");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(150).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Salary).GreaterThanOrEqualTo(0).When(x => x.Salary.HasValue);
    }
}
