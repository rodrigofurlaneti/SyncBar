using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetByInternalMerchantId
{
    public sealed class GetKeetaMerchantMappingByInternalMerchantIdQueryValidator
        : AbstractValidator<GetKeetaMerchantMappingByInternalMerchantIdQuery>
    {
        public GetKeetaMerchantMappingByInternalMerchantIdQueryValidator()
        {
            RuleFor(x => x.InternalMerchantId).NotEmpty()
                .WithMessage("O InternalMerchantId é obrigatório.");
        }
    }
}
