using FluentValidation;

namespace SyncBar.Application.Features.Users.Create;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(200)
            .WithMessage("Senha deve ter no mínimo 8 caracteres.");
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage("Usuário precisa de pelo menos um perfil.");
    }
}
