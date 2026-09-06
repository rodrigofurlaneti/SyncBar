using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Authorization.HandleOAuthCallback
{
    public sealed class HandleKeetaOAuthCallbackCommandValidator : AbstractValidator<HandleKeetaOAuthCallbackCommand>
    {
        public HandleKeetaOAuthCallbackCommandValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.BranchId)
                .GreaterThanOrEqualTo(0)
                .WithMessage("O identificador da filial (BranchId) não pode ser negativo.");

            RuleFor(x => x.AuthId)
                .NotEmpty()
                .WithMessage("O AuthId é obrigatório.");
        }
    }
}
