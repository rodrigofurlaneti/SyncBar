using FluentValidation;

namespace SyncBar.Application.Features.Employees.RegisterTeamMember;

public sealed class RegisterTeamMemberCommandValidator : AbstractValidator<RegisterTeamMemberCommand>
{
    public RegisterTeamMemberCommandValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.JobTitleId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Cpf).NotEmpty().Length(11).Matches("^[0-9]+$").WithMessage("CPF deve ter 11 dígitos numéricos.");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(150).When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Salary).GreaterThanOrEqualTo(0).When(x => x.Salary.HasValue);

        // Campos de acesso ao sistema só são obrigatórios quando o toggle "usa o sistema" está
        // ligado — a maioria dos cargos de equipe (limpeza, vigilância) não precisa deles.
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(100).When(x => x.HasSystemAccess);
        RuleFor(x => x.UserEmail).NotEmpty().EmailAddress().MaximumLength(150).When(x => x.HasSystemAccess);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200)
            .When(x => x.HasSystemAccess)
            .WithMessage("Senha deve ter no mínimo 8 caracteres.");
    }
}
