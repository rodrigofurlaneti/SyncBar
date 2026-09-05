using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetById
{
    public sealed class GetAsaasSavedCardByIdQueryValidator
        : AbstractValidator<GetAsaasSavedCardByIdQuery>
    {
        public GetAsaasSavedCardByIdQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador do cartão (Id) deve ser maior que zero.");
        }
    }
}
