using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Delete
{
    public sealed class DeleteKeetaIntegrationMerchantMappingCommandValidator
        : AbstractValidator<DeleteKeetaIntegrationMerchantMappingCommand>
    {
        public DeleteKeetaIntegrationMerchantMappingCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador do vínculo (Id) deve ser maior que zero.");
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
