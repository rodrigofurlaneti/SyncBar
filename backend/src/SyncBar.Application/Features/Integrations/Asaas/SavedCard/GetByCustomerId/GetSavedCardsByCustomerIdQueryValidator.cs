using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.GetByCustomerId
{
    public sealed class GetSavedCardsByCustomerIdQueryValidator
        : AbstractValidator<GetSavedCardsByCustomerIdQuery>
    {
        public GetSavedCardsByCustomerIdQueryValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("O identificador do cliente (CustomerId) deve ser maior que zero.");
        }
    }
}
