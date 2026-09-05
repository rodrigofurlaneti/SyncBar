using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetByCompanyIdForUpdate
{
    public sealed class GetAsaasSettingByCompanyIdForUpdateQueryValidator
        : AbstractValidator<GetAsaasSettingByCompanyIdForUpdateQuery>
    {
        public GetAsaasSettingByCompanyIdForUpdateQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
