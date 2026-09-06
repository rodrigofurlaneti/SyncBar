using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetAllByBranchId
{
    public sealed class GetAllKeetaMerchantMappingsByBranchIdQueryValidator
        : AbstractValidator<GetAllKeetaMerchantMappingsByBranchIdQuery>
    {
        public GetAllKeetaMerchantMappingsByBranchIdQueryValidator()
        {
            RuleFor(x => x.BranchId).GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
        }
    }
}
