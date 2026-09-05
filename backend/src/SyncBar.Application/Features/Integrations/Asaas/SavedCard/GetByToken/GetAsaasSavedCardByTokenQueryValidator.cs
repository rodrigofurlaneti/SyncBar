using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByToken
{
    public sealed class GetAsaasSavedCardByTokenQueryValidator
        : AbstractValidator<GetAsaasSavedCardByTokenQuery>
    {
        public GetAsaasSavedCardByTokenQueryValidator()
        {
            RuleFor(x => x.CreditCardToken)
                .NotEmpty()
                .WithMessage("O CreditCardToken é obrigatório.")
                .MaximumLength(150)
                .WithMessage("O CreditCardToken deve ter no máximo 150 caracteres.");
        }
    }
}
