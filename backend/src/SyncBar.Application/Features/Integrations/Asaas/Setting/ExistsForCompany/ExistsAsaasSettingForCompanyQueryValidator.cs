using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForCompany
{
    public sealed class ExistsAsaasSettingForCompanyQueryValidator
        : AbstractValidator<ExistsAsaasSettingForCompanyQuery>
    {
        public ExistsAsaasSettingForCompanyQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
