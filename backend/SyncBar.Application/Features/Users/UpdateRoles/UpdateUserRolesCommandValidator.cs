using FluentValidation;

namespace SyncBar.Application.Features.Users.UpdateRoles;

public sealed class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
{
    public UpdateUserRolesCommandValidator()
    {
        RuleFor(x => x.AppUserId).GreaterThan(0);
        RuleFor(x => x.RoleIds).NotEmpty().WithMessage("Usuário precisa de pelo menos um perfil.");
    }
}
