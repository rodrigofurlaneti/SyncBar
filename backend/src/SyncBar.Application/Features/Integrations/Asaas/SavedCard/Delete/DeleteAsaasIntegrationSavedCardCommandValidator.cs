using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.SavedCard.Delete
{
    public sealed class DeleteAsaasIntegrationSavedCardCommandValidator : AbstractValidator<DeleteAsaasIntegrationSavedCardCommand>
    {
        public DeleteAsaasIntegrationSavedCardCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("O identificador do cartão (Id) deve ser maior que zero.");

            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("O identificador do cliente (CustomerId) deve ser maior que zero.");

            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
