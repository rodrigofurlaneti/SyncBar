using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.Update
{
    public sealed class UpdateKeetaIntegrationAuthorizationSessionCommandValidator
        : AbstractValidator<UpdateKeetaIntegrationAuthorizationSessionCommand>
    {
        public UpdateKeetaIntegrationAuthorizationSessionCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador da sessão (Id) deve ser maior que zero.");
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
