using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByIdForUpdate
{
    public sealed class GetAsaasSavedCardByIdForUpdateQueryValidator
        : AbstractValidator<GetAsaasSavedCardByIdForUpdateQuery>
    {
        public GetAsaasSavedCardByIdForUpdateQueryValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador do cartão (Id) deve ser maior que zero.");
        }
    }
}
