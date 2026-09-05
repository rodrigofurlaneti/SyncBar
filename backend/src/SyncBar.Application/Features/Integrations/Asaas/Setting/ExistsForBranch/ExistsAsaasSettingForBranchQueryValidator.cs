using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Asaas.Setting.ExistsForBranch
{
    public sealed class ExistsAsaasSettingForBranchQueryValidator
        : AbstractValidator<ExistsAsaasSettingForBranchQuery>
    {
        public ExistsAsaasSettingForBranchQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");

            RuleFor(x => x.BranchId)
                .GreaterThan(0)
                .WithMessage("O identificador da filial (BranchId) deve ser maior que zero.");
        }
    }
}
