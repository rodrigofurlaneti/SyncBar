using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.Authorization.RequestAuthorizationUrl
{
    public sealed class RequestKeetaAuthorizationUrlCommandValidator
        : AbstractValidator<RequestKeetaAuthorizationUrlCommand>
    {
        public RequestKeetaAuthorizationUrlCommandValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.BranchId)
                .GreaterThanOrEqualTo(0)
                .WithMessage("O identificador da filial (BranchId) não pode ser negativo.");

            RuleFor(x => x.RedirectUri)
                .NotEmpty()
                .WithMessage("O RedirectUri é obrigatório.");
        }
    }
}
