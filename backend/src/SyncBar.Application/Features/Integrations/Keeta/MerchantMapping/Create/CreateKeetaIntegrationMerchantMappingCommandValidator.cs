using FluentValidation;

namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.Create
{
    public sealed class CreateKeetaIntegrationMerchantMappingCommandValidator
        : AbstractValidator<CreateKeetaIntegrationMerchantMappingCommand>
    {
        public CreateKeetaIntegrationMerchantMappingCommandValidator()
        {
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
            RuleFor(x => x.BranchId).GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
            RuleFor(x => x.InternalMerchantId).NotEmpty()
                .WithMessage("O InternalMerchantId é obrigatório.");
            RuleFor(x => x.KeetaMerchantId).GreaterThan(0)
                .WithMessage("O KeetaMerchantId deve ser maior que zero.");
            RuleFor(x => x.StoreName).NotEmpty()
                .WithMessage("O nome da loja (StoreName) é obrigatório.");
        }
    }
}
