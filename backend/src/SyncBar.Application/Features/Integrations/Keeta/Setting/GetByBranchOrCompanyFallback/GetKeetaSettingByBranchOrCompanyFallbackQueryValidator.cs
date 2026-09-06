using FluentValidation;
namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetByBranchOrCompanyFallback
{
    public sealed class GetKeetaSettingByBranchOrCompanyFallbackQueryValidator
        : AbstractValidator<GetKeetaSettingByBranchOrCompanyFallbackQuery>
    {
        public GetKeetaSettingByBranchOrCompanyFallbackQueryValidator()
        {
            RuleFor(x => x.CompanyId)
                .GreaterThan(0)
                .WithMessage("O identificador da empresa (CompanyId) deve ser maior que zero.");
        }
    }
}
