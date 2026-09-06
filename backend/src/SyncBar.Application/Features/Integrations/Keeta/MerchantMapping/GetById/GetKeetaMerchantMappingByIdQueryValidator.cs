using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById
{
    public sealed class GetKeetaMerchantMappingByIdQueryValidator
        : AbstractValidator<GetKeetaMerchantMappingByIdQuery>
    {
        public GetKeetaMerchantMappingByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0)
                .WithMessage("O identificador do vínculo (Id) deve ser maior que zero.");
        }
    }
}
