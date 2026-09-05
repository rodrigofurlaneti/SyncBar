using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByCompanyId
{
    public sealed class GetAsaasSettingByCompanyIdQueryValidator
        : AbstractValidator<GetAsaasSettingByCompanyIdQuery>
    {
        public GetAsaasSettingByCompanyIdQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
