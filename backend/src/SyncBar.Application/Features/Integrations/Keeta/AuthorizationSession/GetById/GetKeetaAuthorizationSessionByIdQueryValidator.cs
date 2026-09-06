using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetById
{
    public sealed class GetKeetaAuthorizationSessionByIdQueryValidator
        : AbstractValidator<GetKeetaAuthorizationSessionByIdQuery>
    {
        public GetKeetaAuthorizationSessionByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador da sessão (Id) deve ser maior que zero.");
        }
    }
}
