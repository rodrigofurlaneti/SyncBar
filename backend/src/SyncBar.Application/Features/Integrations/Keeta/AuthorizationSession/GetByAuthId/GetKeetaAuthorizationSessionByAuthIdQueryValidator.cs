using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.AuthorizationSession.GetByAuthId
{
    public sealed class GetKeetaAuthorizationSessionByAuthIdQueryValidator
        : AbstractValidator<GetKeetaAuthorizationSessionByAuthIdQuery>
    {
        public GetKeetaAuthorizationSessionByAuthIdQueryValidator()
        {
            RuleFor(x => x.AuthId).NotEmpty()
                .WithMessage("O AuthId é obrigatório.");
        }
    }
}
