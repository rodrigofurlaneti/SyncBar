using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.GetAllActive
{
    public sealed class GetAllActiveAsaasSettingsQueryValidator
        : AbstractValidator<GetAllActiveAsaasSettingsQuery>
    {
        public GetAllActiveAsaasSettingsQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
