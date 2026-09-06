using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByCompanyId
{
    public sealed class GetAllKeetaMerchantMappingsByCompanyIdQueryValidator
        : AbstractValidator<GetAllKeetaMerchantMappingsByCompanyIdQuery>
    {
        public GetAllKeetaMerchantMappingsByCompanyIdQueryValidator()
        {
            RuleFor(x => x.CompanyId).GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
