using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Create
{
    public sealed class CreateKeetaIntegrationAuthorizationSessionCommandValidator
        : AbstractValidator<CreateKeetaIntegrationAuthorizationSessionCommand>
    {
        public CreateKeetaIntegrationAuthorizationSessionCommandValidator()
        {
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
            RuleFor(x => x.BranchId).GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
            RuleFor(x => x.AuthId).NotEmpty()
                .WithMessage("O AuthId é obrigatório.");
        }
    }
}
